/*
Copyright 2017 Coin Foundry (coinfoundry.org)
Authors: Oliver Weichhold (oliver@weichhold.com)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial
portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Numerics;
using Autofac;
using AutoMapper;
using Miningcore.Blockchain.Aion;
using Miningcore.Blockchain.Ethereum.Configuration;
using Miningcore.Persistence.Model;
using Miningcore.Notifications.Messages;
using Miningcore.Configuration;
using Miningcore.Extensions;
using Miningcore.JsonRpc;
using Miningcore.Messaging;
using Miningcore.Mining;
using Miningcore.Notifications;
using Miningcore.Persistence;
using Miningcore.Persistence.Repositories;
using Miningcore.Stratum;
using Miningcore.Time;
using Miningcore.Util;
using Newtonsoft.Json;
using NBitcoin;
using Miningcore.Contracts;
using System.Collections;

namespace Miningcore.Blockchain.Aion
{
    [CoinFamily(CoinFamily.Aion)]
    public class AionPool : PoolBase
    {
        public AionPool(IComponentContext ctx,
            JsonSerializerSettings serializerSettings,
            IConnectionFactory cf,
            IStatsRepository statsRepo,
            IMapper mapper,
            IMasterClock clock,
            IMessageBus messageBus) :
            base(ctx, serializerSettings, cf, statsRepo, mapper, clock, messageBus)
        {
        }

        private object currentJobParams;
        private AionJobManager manager;
        protected static readonly Regex regexMinimumPayment = new Regex(@";?mp=(\d*(\.\d+)?)", RegexOptions.Compiled);

        private async Task OnSubscribeAsync(StratumClient client, Timestamped<JsonRpcRequest> tsRequest)
        {
            var request = tsRequest.Value;
            var context = client.ContextAs<AionWorkerContext>();

            if (request.Id == null)
                throw new StratumException(StratumError.Other, "missing request id");

            var requestParams = request.ParamsAs<string[]>();

            if (requestParams == null || requestParams.Length < 2)
                throw new StratumException(StratumError.MinusOne, "invalid request");

            manager.PrepareWorker(client);
            var data = new object[]
                {
                    new object[]
                    {
                        AionStratumMethods.MiningNotify,
                        client.ConnectionId,
                        AionConstants.AionStratumVersion
                    },
                    context.ExtraNonce1
                }
                .ToArray();

            await client.RespondAsync(data, request.Id);

            // setup worker context
            context.IsSubscribed = true;
            context.UserAgent = requestParams[0].Trim();
        }

        private async Task OnAuthorizeAsync(StratumClient client, Timestamped<JsonRpcRequest> tsRequest)
        {
            var request = tsRequest.Value;
            var context = client.ContextAs<AionWorkerContext>();

            if (request.Id == null)
                throw new StratumException(StratumError.MinusOne, "missing request id");

            var requestParams = request.ParamsAs<string[]>();
            var workerValue = requestParams?.Length > 0 ? requestParams[0] : null;
            var password = requestParams?.Length > 1 ? requestParams[1] : null;
            var passParts = password?.Split(PasswordControlVarsSeparator);

            // extract worker/miner
            var workerParts = workerValue?.Split('.');
            var minerName = workerParts?.Length > 0 ? workerParts[0].Trim() : null;
            var workerName = workerParts?.Length > 1 ? workerParts[1].Trim() : null;
            var minimumPayment = GetMinimumPaymentFromPassparts(passParts);

            // assumes that workerName is an address
            context.IsAuthorized = !string.IsNullOrEmpty(minerName) && await manager.ValidateAddressAsync(minerName);
            context.MinerName = minerName;
            context.WorkerName = workerName;
            // respond
            await client.RespondAsync(context.IsAuthorized, request.Id);

            // extract control vars from password
            var staticDiff = GetStaticDiffFromPassparts(passParts);
            if (staticDiff.HasValue &&
                (context.VarDiff != null && staticDiff.Value >= context.VarDiff.Config.MinDiff ||
                context.VarDiff == null && staticDiff.Value > context.Difficulty))
            {
                context.VarDiff = null; // disable vardiff
                context.SetDifficulty(staticDiff.Value);
            }

            messageBus.SendMessage(new MinerInfo(poolConfig.Id, 
                context.MinerName, context.MinimumPayment = minimumPayment));

            await EnsureInitialWorkSent(client);

            // log association
            logger.Info(() => $"[{client.ConnectionId}] Authorized worker {workerValue}");
        }

        private async Task OnSubmitAsync(StratumClient client, Timestamped<JsonRpcRequest> tsRequest)
        {
            var request = tsRequest.Value;
            var context = client.ContextAs<AionWorkerContext>();

            try
            {
                if (request.Id == null)
                    throw new StratumException(StratumError.MinusOne, "missing request id");

                // check age of submission (aged submissions are usually caused by high server load)
                var requestAge = clock.Now - tsRequest.Timestamp.UtcDateTime;

                if (requestAge > maxShareAge)
                {
                    logger.Warn(() => $"[{client.ConnectionId}] Dropping stale share submission request (server overloaded?)");
                    return;
                }

                // validate worker
                if (!context.IsAuthorized)
                    throw new StratumException(StratumError.UnauthorizedWorker, "Unauthorized worker");
                else if (!context.IsSubscribed)
                    throw new StratumException(StratumError.NotSubscribed, "Not subscribed");

                // check request
                var submitRequest = request.ParamsAs<string[]>();

                if (submitRequest.Length != 5 || submitRequest.Any(string.IsNullOrEmpty))
                    throw new StratumException(StratumError.MinusOne, "malformed PoW result");

                // recognize activity
                context.LastActivity = clock.Now;

                var poolEndpoint = poolConfig.Ports[client.PoolEndpoint.Port];
                try
                {
                    var share = await manager.SubmitShareAsync(client, submitRequest, context.Difficulty, poolEndpoint.Difficulty);
                    // success
                    await client.RespondAsync(true, request.Id);
                    // publish
                    messageBus.SendMessage(new ClientShare(client, share));
                    // telemetry
                    PublishTelemetry(TelemetryCategory.Share, clock.Now - tsRequest.Timestamp.UtcDateTime, true);

                    logger.Debug(() => $"[{client.ConnectionId}] Share accepted: D={Math.Round(share.Difficulty, 3)}");
                    await EnsureInitialWorkSent(client);

                    // update pool stats
                    if (share.IsBlockCandidate)
                        poolStats.LastPoolBlockTime = clock.Now;

                    context.Stats.ValidShares++;
                    await UpdateVarDiffAsync(client);
                }
                catch (Miningcore.Stratum.StratumException ex)
                {
                    // telemetry
                    PublishTelemetry(TelemetryCategory.Share, clock.Now - tsRequest.Timestamp.UtcDateTime, false);

                    // update client stats
                    context.Stats.InvalidShares++;

                    logger.Info(() => $"[{client.ConnectionId}] Share rejected: {ex.Message}");

                    // banning
                    ConsiderBan(client, context, poolConfig.Banning);

                    throw;
                }
            }
            catch (StratumException ex)
            {
                await client.RespondErrorAsync(ex.Code, ex.Message, request.Id, false);
                messageBus.SendMessage(new InvalidShare
                {
                    PoolId = poolConfig.Id,
                    Miner = context.MinerName,
                    Worker = context.WorkerName,
                    Created = clock.Now
                });
                // telemetry
                PublishTelemetry(TelemetryCategory.Share, clock.Now - tsRequest.Timestamp.UtcDateTime, false);

                // update client stats
                context.Stats.InvalidShares++;
                logger.Info(() => $"[{client.ConnectionId}] Share rejected: {ex.Message}");

                // banning
                ConsiderBan(client, context, poolConfig.Banning);

                throw;
            }
        }

        private async Task EnsureInitialWorkSent(StratumClient client)
        {
            var context = client.ContextAs<AionWorkerContext>();
            ArrayList arrayTarget = new ArrayList();
            var sendInitialWork = false;

            lock (context)
            {
                if (context.IsSubscribed && context.IsAuthorized && !context.IsInitialWorkSent)
                {
                    context.IsInitialWorkSent = true;
                    string newTarget = AionUtils.diffToTarget(context.Difficulty);
                    arrayTarget.Add(newTarget);
                    sendInitialWork = true;
                }
            }

            if (sendInitialWork)
            {
                // send intial update
                await client.NotifyAsync(AionStratumMethods.MiningNotify, currentJobParams);
                await client.NotifyAsync(AionStratumMethods.SetTarget, arrayTarget);
            }
        }

        protected virtual Task OnNewJobAsync(object jobParams)
        {
            currentJobParams = jobParams;

            logger.Debug(() => $"Broadcasting job");

            var tasks = ForEachClient(async client =>
            {
                var context = client.ContextAs<AionWorkerContext>();

                if (context.IsSubscribed && context.IsAuthorized && context.IsInitialWorkSent)
                {
                    // check alive
                    var lastActivityAgo = clock.Now - context.LastActivity;

                    if (poolConfig.ClientConnectionTimeout > 0 &&
                        lastActivityAgo.TotalSeconds > poolConfig.ClientConnectionTimeout)
                    {
                        logger.Info(() => $"Booting zombie-worker (idle-timeout exceeded)");
                        DisconnectClient(client);
                        return;
                    }

                    // varDiff: if the client has a pending difficulty change, apply it now
                    if (context.ApplyPendingDifficulty())
                        await client.NotifyAsync(AionStratumMethods.SetDifficulty, new object[] { context.Difficulty });

                    string newTarget = AionUtils.diffToTarget(context.Difficulty);
                    ArrayList arrayTarget = new ArrayList();
                    arrayTarget.Add(newTarget);

                    await client.NotifyAsync(AionStratumMethods.MiningNotify, currentJobParams);
                    await client.NotifyAsync(AionStratumMethods.SetTarget, arrayTarget);
                }
            });

            return Task.WhenAll(tasks);
        }

        #region Overrides
        protected override async Task SetupJobManager(CancellationToken ct)
        {
            manager = ctx.Resolve<AionJobManager>();
            manager.Configure(poolConfig, clusterConfig);

            var isNodeRunning = false;
            while (!isNodeRunning)
            {
                isNodeRunning = await manager.IsDaemonRunning();
                if (!isNodeRunning)
                {
                    logger.Info(() => $"No daemon is running. Checking again in 1 minute");
                    Thread.Sleep(1000 * 60 * 1); // 1 Minute
                }
            }

            ValidatePoolAddress();

            await manager.StartAsync(ct);

            if (poolConfig.EnableInternalStratum == true)
            {
                disposables.Add(manager.Jobs
                    .Select(job => Observable.FromAsync(async () =>
                    {
                        try
                        {
                            await OnNewJobAsync(job);
                        }

                        catch (Exception ex)
                        {
                            logger.Debug(() => $"{nameof(OnNewJobAsync)}: {ex.Message}");
                        }
                    }))
                    .Concat()
                    .Subscribe(_ => { }, ex =>
                    {
                        logger.Debug(ex, nameof(OnNewJobAsync));
                    }));

                // // we need work before opening the gates
                await manager.Jobs.Take(1).ToTask(ct);
            }
            else
            {
                // keep updating NetworkStats
                disposables.Add(manager.Jobs.Subscribe());
            }
        }

        private async void ValidatePoolAddress()
        {
            var poolAddressValid = await manager.ValidateAddressAsync(poolConfig.Address);
            if (!poolAddressValid)
            {
                logger.ThrowLogPoolStartupException("Invalid pool address. Please check your configuration file.");
            }
        }

        private Decimal GetMinimumPaymentFromPassparts(string[] parts)
        {
            if (parts == null || parts.Length == 0)
                return 0;

            foreach(var part in parts)
            {
                var m = regexMinimumPayment.Match(part);

                if (m.Success)
                {
                    var str = m.Groups[1].Value.Trim();
                    return Decimal.Parse(str);
                }
            }

            return 0;
        }

        protected override async Task InitStatsAsync()
        {
            await base.InitStatsAsync();

            blockchainStats = manager.BlockchainStats;
        }


        protected override WorkerContextBase CreateClientContext()
        {
            return new AionWorkerContext();
        }

        protected override async Task OnRequestAsync(StratumClient client,
            Timestamped<JsonRpcRequest> tsRequest, CancellationToken ct)
        {
            var request = tsRequest.Value;
            try
            {
                switch (request.Method)
                {
                    case AionStratumMethods.Subscribe:
                        await OnSubscribeAsync(client, tsRequest);
                        break;

                    case AionStratumMethods.Authorize:
                        await OnAuthorizeAsync(client, tsRequest);
                        break;

                    case AionStratumMethods.SubmitShare:
                        await OnSubmitAsync(client, tsRequest);
                        break;
                    default:
                        logger.Debug(() => $"[{client.ConnectionId}] Unsupported RPC request: {JsonConvert.SerializeObject(request, serializerSettings)}");

                        await client.RespondErrorAsync(StratumError.Other, $"Unsupported request {request.Method}", request.Id);
                        break;
                }
            }
            catch (StratumException ex)
            {
                await client.RespondErrorAsync(ex.Code, ex.Message, request.Id, false);
            }
        }

        public override double HashrateFromShares(double shares, double interval)
        {
            var result = shares / interval;
            return result;
        }

        protected override async Task OnVarDiffUpdateAsync(StratumClient client, double newDiff)
        {
            await base.OnVarDiffUpdateAsync(client, newDiff);

            // apply immediately and notify client
            var context = client.ContextAs<AionWorkerContext>();

            if (context.HasPendingDifficulty)
            {
                context.ApplyPendingDifficulty();
                string newTarget = AionUtils.diffToTarget(newDiff);
                ArrayList targetArray = new ArrayList();
                targetArray.Add(newTarget);

                // send job
                await client.NotifyAsync(AionStratumMethods.MiningNotify, currentJobParams);
                await client.NotifyAsync(AionStratumMethods.SetTarget, targetArray);
            }
        }

        public override void Configure(PoolConfig poolConfig, ClusterConfig clusterConfig)
        {
            base.Configure(poolConfig, clusterConfig);

            // validate mandatory extra config
            // TODO: update here in case it's needed
        }

        #endregion // Overrides
    }
}
