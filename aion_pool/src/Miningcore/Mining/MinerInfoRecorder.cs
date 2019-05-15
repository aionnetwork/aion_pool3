using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Miningcore.Configuration;
using Miningcore.Extensions;
using Miningcore.Messaging;
using Miningcore.Notifications;
using Miningcore.Notifications.Messages;
using Miningcore.Persistence;
using Miningcore.Blockchain;
using Miningcore.Persistence.Repositories;
using Miningcore.Time;
using Miningcore.Util;
using Newtonsoft.Json;
using NLog;
using Polly;
using Polly.CircuitBreaker;
using Contract = Miningcore.Contracts.Contract;

namespace Miningcore.Mining
{
    /// <summary>
    /// Asynchronously persist miner infos produced by all pools for processing by coin-specific payment processor(s)
    /// </summary>
    public class MinerInfoRecorder
    {
        public MinerInfoRecorder(IConnectionFactory cf, IMapper mapper,
            JsonSerializerSettings jsonSerializerSettings,
            IMinerInfoRepository minerInfoRepository,
            IMasterClock clock,
            IMessageBus messageBus,
            NotificationService notificationService)
        {
            Contract.RequiresNonNull(cf, nameof(cf));
            Contract.RequiresNonNull(mapper, nameof(mapper));
            Contract.RequiresNonNull(minerInfoRepository, nameof(minerInfoRepository));
            Contract.RequiresNonNull(jsonSerializerSettings, nameof(jsonSerializerSettings));
            Contract.RequiresNonNull(clock, nameof(clock));
            Contract.RequiresNonNull(messageBus, nameof(messageBus));
            Contract.RequiresNonNull(notificationService, nameof(notificationService));

            this.cf = cf;
            this.mapper = mapper;
            this.jsonSerializerSettings = jsonSerializerSettings;
            this.clock = clock;
            this.messageBus = messageBus;
            this.notificationService = notificationService;

            this.minerInfoRepository = minerInfoRepository;

            BuildFaultHandlingPolicy();
        }

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IMinerInfoRepository minerInfoRepository;
        private readonly IConnectionFactory cf;
        private readonly JsonSerializerSettings jsonSerializerSettings;
        private readonly IMasterClock clock;
        private readonly IMessageBus messageBus;
        private readonly NotificationService notificationService;
        private ClusterConfig clusterConfig;
        private readonly IMapper mapper;
        private readonly BlockingCollection<MinerInfo> queue = new BlockingCollection<MinerInfo>();

        private readonly int QueueSizeWarningThreshold = 1024;
        private readonly TimeSpan relayReceiveTimeout = TimeSpan.FromSeconds(60);
        private Policy faultPolicy;
        private bool hasWarnedAboutBacklogSize;
        private IDisposable queueSub;
        private const int RetryCount = 3;
        private const string PolicyContextKeyMinerInfo = "minerInfo";

        private void PersistMinerInfosFaulTolerant(IList<MinerInfo> minerInfos)
        {
            var context = new Dictionary<string, object> { { PolicyContextKeyMinerInfo, minerInfos } };

            faultPolicy.Execute(() => AddOrUpdateInfos(minerInfos));
        }

        private async Task AddOrUpdateInfos(IList<MinerInfo> minerInfos)
        {
            await cf.RunTx(async (con, tx) =>
            {
                foreach(var minerInfo in minerInfos)
                {
                    var minerInfoEntity = mapper.Map<Miningcore.Persistence.Model.MinerInfo>(minerInfo);
                    await AddOrUpdateMinerInfo(minerInfoEntity);
                }
            });
        }

        private static void OnPolicyRetry(Exception ex, TimeSpan timeSpan, int retry, object context)
        {
            logger.Warn(() => $"Retry {retry} in {timeSpan} due to {ex.Source}: {ex.GetType().Name} ({ex.Message})");
        }

        private void OnPolicyFallback(Exception ex, Context context)
        {
            logger.Warn(() => $"Fallback due to {ex.Source}: {ex.GetType().Name} ({ex.Message})");
        }

        #region API-Surface

        public void Start(ClusterConfig clusterConfig)
        {
            this.clusterConfig = clusterConfig;

            InitializeQueue();

            logger.Info(() => "Online");
        }

        public void Stop()
        {
            logger.Info(() => "Stopping ..");

            queueSub.Dispose();
            queue.Dispose();

            logger.Info(() => "Stopped");
        }

        #endregion // API-Surface

        private void InitializeQueue()
        {
            messageBus.Listen<MinerInfo>().Subscribe(x => queue.Add(x));

            queueSub = queue.GetConsumingEnumerable()
                .ToObservable(TaskPoolScheduler.Default)
                .Do(_ => CheckQueueBacklog())
                .Buffer(TimeSpan.FromSeconds(1), 100)
                .Where(minerInfos => minerInfos.Any())
                .Subscribe(minerInfos =>
                {
                    try
                    {
                        PersistMinerInfosFaulTolerant(minerInfos);
                    }

                    catch(Exception ex)
                    {
                        logger.Error(ex);
                    }
                });
        }

        private async Task AddOrUpdateMinerInfo(Miningcore.Persistence.Model.MinerInfo minerInfo)
        {   
            if(minerInfo.MinimumPayment == 0) 
            {
                await deleteMinerInfo(minerInfo);
                return;
            }
            
            Miningcore.Persistence.Model.MinerInfo retrieved = await cf.RunTx(async (con, tx) =>
            {
                return await minerInfoRepository.GetMinerInfo(con, tx, minerInfo.PoolId, minerInfo.Miner);
            });

            if (minerInfo != null)
            {
                await cf.RunTx(async (con, tx) =>
                {
                    await minerInfoRepository.DeleteMinerInfo(con, tx, minerInfo.PoolId, minerInfo.Miner);
                });
            }

            await cf.RunTx(async (con, tx) =>
            {
                await minerInfoRepository.AddMinerInfo(con, tx, minerInfo.PoolId, minerInfo.Miner, minerInfo.MinimumPayment);
            });
        }

        private async Task deleteMinerInfo(Miningcore.Persistence.Model.MinerInfo context) {
            await cf.RunTx(async (con, tx) =>
                {
                    await minerInfoRepository.DeleteMinerInfo(con, tx, context.PoolId, context.Miner);
                });
        }

        private void CheckQueueBacklog()
        {
            if (queue.Count > QueueSizeWarningThreshold)
            {
                if (!hasWarnedAboutBacklogSize)
                {
                    logger.Warn(() => $"Miner info queue backlog has crossed {QueueSizeWarningThreshold}");
                    hasWarnedAboutBacklogSize = true;
                }
            }

            else if (hasWarnedAboutBacklogSize && queue.Count <= QueueSizeWarningThreshold / 2)
            {
                hasWarnedAboutBacklogSize = false;
            }
        }

        private void BuildFaultHandlingPolicy()
        {
            // retry with increasing delay (1s, 2s, 4s etc)
            var retry = Policy
                .Handle<DbException>()
                .Or<SocketException>()
                .Or<TimeoutException>()
                .WaitAndRetry(RetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    OnPolicyRetry);

            // after retries failed several times, break the circuit and fall through to
            // fallback action for one minute, not attempting further retries during that period
            var breaker = Policy
                .Handle<DbException>()
                .Or<SocketException>()
                .Or<TimeoutException>()
                .CircuitBreaker(2, TimeSpan.FromMinutes(1));

            faultPolicy = Policy.Wrap(breaker, retry);
        }
    }
}
