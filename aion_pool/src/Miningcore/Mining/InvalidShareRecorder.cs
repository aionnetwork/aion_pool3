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
using AutoMapper;
using Miningcore.Configuration;
using Miningcore.Extensions;
using Miningcore.Messaging;
using Miningcore.Notifications;
using Miningcore.Notifications.Messages;
using Miningcore.Persistence;
using Miningcore.Persistence.Model;
using Miningcore.Persistence.Repositories;
using Miningcore.Time;
using Miningcore.Util;
using Newtonsoft.Json;
using NLog;
using Polly;
using Polly.CircuitBreaker;
using Contract = Miningcore.Contracts.Contract;
using InvalidShare = Miningcore.Blockchain.InvalidShare;

namespace Miningcore.Mining
{
    /// <summary>
    /// Asynchronously persist invalid shares produced by all pools for processing by coin-specific payment processor(s)
    /// </summary>
    public class InvalidShareRecorder
    {
        public InvalidShareRecorder(IConnectionFactory cf, IMapper mapper,
            JsonSerializerSettings jsonSerializerSettings,
            IInvalidShareRepository shareRepo,
            IMasterClock clock,
            IMessageBus messageBus,
            NotificationService notificationService)
        {
            Contract.RequiresNonNull(cf, nameof(cf));
            Contract.RequiresNonNull(mapper, nameof(mapper));
            Contract.RequiresNonNull(shareRepo, nameof(shareRepo));
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

            this.shareRepo = shareRepo;

            BuildFaultHandlingPolicy();
        }

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IInvalidShareRepository shareRepo;
        private readonly IConnectionFactory cf;
        private readonly JsonSerializerSettings jsonSerializerSettings;
        private readonly IMasterClock clock;
        private readonly IMessageBus messageBus;
        private readonly NotificationService notificationService;
        private ClusterConfig clusterConfig;
        private readonly IMapper mapper;
        private readonly BlockingCollection<InvalidShare> queue = new BlockingCollection<InvalidShare>();

        private readonly int QueueSizeWarningThreshold = 1024;
        private readonly TimeSpan relayReceiveTimeout = TimeSpan.FromSeconds(60);
        private Policy faultPolicy;
        private bool hasLoggedPolicyFallbackFailure;
        private bool hasWarnedAboutBacklogSize;
        private IDisposable queueSub;
        private const int RetryCount = 3;
        private const string PolicyContextKeyShares = "invalidShare";
        private bool notifiedAdminOnPolicyFallback = false;

        private void PersistInvalidSharesFaulTolerant(IList<InvalidShare> shares)
        {
            var context = new Dictionary<string, object> { { PolicyContextKeyShares, shares } };

            faultPolicy.Execute(() => PersistShares(shares));
        }

        private void PersistShares(IList<InvalidShare> shares)
        {
            cf.RunTx(async (con, tx) =>
            {
                foreach(var share in shares)
                {
                    var shareEntity = mapper.Map<Miningcore.Persistence.Model.InvalidShare>(share);
                    shareRepo.Insert(con, tx, shareEntity);
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
            messageBus.Listen<InvalidShare>().Subscribe(x => queue.Add(x));

            queueSub = queue.GetConsumingEnumerable()
                .ToObservable(TaskPoolScheduler.Default)
                .Do(_ => CheckQueueBacklog())
                .Buffer(TimeSpan.FromSeconds(1), 100)
                .Where(shares => shares.Any())
                .Subscribe(shares =>
                {
                    try
                    {
                        PersistInvalidSharesFaulTolerant(shares);
                    }

                    catch(Exception ex)
                    {
                        logger.Error(ex);
                    }
                });
        }

        private void CheckQueueBacklog()
        {
            if (queue.Count > QueueSizeWarningThreshold)
            {
                if (!hasWarnedAboutBacklogSize)
                {
                    logger.Warn(() => $"Invalid share persistence queue backlog has crossed {QueueSizeWarningThreshold}");
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
