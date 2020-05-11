using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Miningcore.Blockchain;
using Miningcore.Configuration;
using Miningcore.Contracts;
using Miningcore.Extensions;
using Miningcore.Messaging;
using Miningcore.Util;
using Newtonsoft.Json;
using NLog;
using ProtoBuf;
using ZeroMQ;

namespace Miningcore.Mining
{
    public class Relay
    {
        public Relay(JsonSerializerSettings serializerSettings, IMessageBus messageBus)
        {
            Contract.RequiresNonNull(serializerSettings, nameof(serializerSettings));
            Contract.RequiresNonNull(messageBus, nameof(messageBus));

            this.serializerSettings = serializerSettings;
            this.messageBus = messageBus;
        }

        private readonly IMessageBus messageBus;
        private ClusterConfig clusterConfig;
        private readonly BlockingCollection<Share> shareQueue = new BlockingCollection<Share>();
        private readonly BlockingCollection<InvalidShare> invalidShareQueue = new BlockingCollection<InvalidShare>();
        private readonly BlockingCollection<MinerInfo> minerInfoQueue = new BlockingCollection<MinerInfo>();
        private IDisposable shareQueueSub;
        private IDisposable invalidShareQueueSub;
        private IDisposable minerInfoQueueSub;
        private readonly int QueueSizeWarningThreshold = 1024;
        private bool hasWarnedAboutBacklogSize;
        private ZSocket pubSocket;
        private readonly JsonSerializerSettings serializerSettings;

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        #region API-Surface

        public void Start(ClusterConfig clusterConfig)
        {
            this.clusterConfig = clusterConfig;

            messageBus.Listen<ClientShare>().Subscribe(x => shareQueue.Add(x.Share));
            messageBus.Listen<InvalidShare>().Subscribe(x => invalidShareQueue.Add(x));
            messageBus.Listen<MinerInfo>().Subscribe(x => minerInfoQueue.Add(x));

            pubSocket = new ZSocket(ZSocketType.PUB);

            if (!clusterConfig.Relay.Connect)
            {
                // pubSocket.SetupCurveTlsServer(clusterConfig.Relay.SharedEncryptionKey, logger);

                pubSocket.Bind(clusterConfig.Relay.PublishUrl);

                if(pubSocket.CurveServer)
                    logger.Info(() => $"Bound to {clusterConfig.Relay.PublishUrl} using Curve public-key {pubSocket.CurvePublicKey.ToHexString()}");
                else
                    logger.Info(() => $"Bound to {clusterConfig.Relay.PublishUrl}");
            }

            else
            {
                if(!string.IsNullOrEmpty(clusterConfig.Relay.SharedEncryptionKey?.Trim()))
                    logger.ThrowLogPoolStartupException("ZeroMQ Curve is not supported in Relay Connect-Mode");

                pubSocket.Connect(clusterConfig.Relay.PublishUrl);
                logger.Info(() => $"Connected to {clusterConfig.Relay.PublishUrl}");
            }

            InitializeQueues();

            logger.Info(() => "Online");
        }

        public void Stop()
        {
            logger.Info(() => "Stopping ..");

            pubSocket.Dispose();

            shareQueueSub?.Dispose();
            shareQueueSub = null;
            invalidShareQueueSub?.Dispose();
            invalidShareQueueSub = null;
            minerInfoQueueSub?.Dispose();
            minerInfoQueueSub = null;

            logger.Info(() => "Stopped");
        }

        #endregion // API-Surface

        private void InitializeQueues()
        {
            InitializeShareQueue();
            InitializeInvalidShareQueue();
            InitializeMinerInfoQueue();
        }

        private void InitializeShareQueue() 
        {
            shareQueueSub = shareQueue.GetConsumingEnumerable()
                .ToObservable(TaskPoolScheduler.Default)
                .Do(_ => CheckQueueBacklog())
                .Subscribe(share =>
                {
                    share.Source = clusterConfig.ClusterName;
                    share.BlockRewardDouble = (double) share.BlockReward;

                    try
                    {
                        var flags = (int) RelayInfo.WireFormat.ProtocolBuffers;

                        using (var msg = new ZMessage())
                        {
                            // Topic frame
                            msg.Add(new ZFrame(share.PoolId));

                            // Frame 2: flags
                            msg.Add(new ZFrame(flags));

                            // Frame 3: content type
                            msg.Add(new ZFrame(RelayContentType.Share.ToString()));

                            // Frame 4: payload
                            using(var stream = new MemoryStream())
                            {
                                Serializer.Serialize(stream, share);
                                msg.Add(new ZFrame(stream.ToArray()));
                            }

                            pubSocket.SendMessage(msg);
                        }
                    }

                    catch(Exception ex)
                    {
                        logger.Error(ex);
                    }
                });
        }

        private void InitializeInvalidShareQueue()
        {
            invalidShareQueueSub = invalidShareQueue.GetConsumingEnumerable()
                .ToObservable(TaskPoolScheduler.Default)
                .Do(_ => CheckQueueBacklog())
                .Subscribe(invalidShare =>
                {
                    try
                    {
                        var flags = (int) RelayInfo.WireFormat.ProtocolBuffers;

                        using (var msg = new ZMessage())
                        {
                            // Topic frame
                            msg.Add(new ZFrame(invalidShare.PoolId));

                            // Frame 2: flags
                            msg.Add(new ZFrame(flags));

                            // Frame 3: content type
                            msg.Add(new ZFrame(RelayContentType.InvalidShare.ToString()));

                            // Frame 4: payload
                            using(var stream = new MemoryStream())
                            {
                                Serializer.Serialize(stream, invalidShare);
                                msg.Add(new ZFrame(stream.ToArray()));
                            }

                            pubSocket.SendMessage(msg);
                        }
                    }

                    catch(Exception ex)
                    {
                        logger.Error(ex);
                    }
                });
        }

        private void InitializeMinerInfoQueue()
        {
            minerInfoQueueSub = minerInfoQueue.GetConsumingEnumerable()
                .ToObservable(TaskPoolScheduler.Default)
                .Do(_ => CheckQueueBacklog())
                .Subscribe(minerInfo =>
                {
                    try
                    {
                        var flags = (int) RelayInfo.WireFormat.ProtocolBuffers;

                        using (var msg = new ZMessage())
                        {
                            // Topic frame
                            msg.Add(new ZFrame(minerInfo.PoolId));

                            // Frame 2: flags
                            msg.Add(new ZFrame(flags));

                            // Frame 3: content type
                            msg.Add(new ZFrame(RelayContentType.MinerInfo.ToString()));

                            // Frame 4: payload
                            using(var stream = new MemoryStream())
                            {
                                Serializer.Serialize(stream, minerInfo);
                                msg.Add(new ZFrame(stream.ToArray()));
                            }

                            pubSocket.SendMessage(msg);
                        }
                    }

                    catch(Exception ex)
                    {
                        logger.Error(ex);
                    }
                });
        }
        private void CheckQueueBacklog()
        {
            if (shareQueue.Count > QueueSizeWarningThreshold)
            {
                if (!hasWarnedAboutBacklogSize)
                {
                    logger.Warn(() => $"Share relay queue backlog has crossed {QueueSizeWarningThreshold}");
                    hasWarnedAboutBacklogSize = true;
                }
            }

            else if (hasWarnedAboutBacklogSize && shareQueue.Count <= QueueSizeWarningThreshold / 2)
            {
                hasWarnedAboutBacklogSize = false;
            }
        }
    }
}
