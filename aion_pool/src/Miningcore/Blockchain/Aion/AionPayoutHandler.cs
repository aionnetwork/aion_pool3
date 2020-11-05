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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Globalization;
using Autofac;
using AutoMapper;
using Miningcore.Blockchain.Aion.DaemonRequests;
using Miningcore.Blockchain.Aion.DaemonResponses;
using Miningcore.Blockchain.Aion.Configuration;
using Miningcore.Blockchain.Aion.Transaction;
using Miningcore.Configuration;
using Miningcore.DaemonInterface;
using Miningcore.Extensions;
using Miningcore.Notifications;
using Miningcore.Payments;
using Miningcore.Persistence;
using Miningcore.Persistence.Model;
using Miningcore.Persistence.Repositories;
using Miningcore.Messaging;
using Miningcore.Time;
using Miningcore.Util;
using Newtonsoft.Json;
using Block = Miningcore.Persistence.Model.Block;
using Contract = Miningcore.Contracts.Contract;
using AionCommands = Miningcore.Blockchain.Aion.AionCommands;

namespace Miningcore.Blockchain.Aion
{
    [CoinFamily(CoinFamily.Aion)]
    public class AionPayoutHandler : PayoutHandlerBase,
        IPayoutHandler
    {
        public AionPayoutHandler(
            IComponentContext ctx,
            IConnectionFactory cf,
            IMapper mapper,
            IShareRepository shareRepo,
            IBlockRepository blockRepo,
            IBalanceRepository balanceRepo,
            IPaymentRepository paymentRepo,
            IMasterClock clock,
            IMinerInfoRepository minerInfoRepository,
            IMessageBus messageBus) :
            base(cf, mapper, shareRepo, blockRepo, balanceRepo, paymentRepo, clock, messageBus)
        {
            Contract.RequiresNonNull(ctx, nameof(ctx));
            Contract.RequiresNonNull(balanceRepo, nameof(balanceRepo));
            Contract.RequiresNonNull(paymentRepo, nameof(paymentRepo));

            this.ctx = ctx;
            this.minerInfoRepository = minerInfoRepository;
        }

        private readonly IComponentContext ctx;
        private DaemonClient daemon;
        private int latestNonce = 0;
        private bool usedLatestNonce = false;
        private AionPoolPaymentExtraConfig extraConfig;
        private AionRewardsCalculator rewardsCalculator;

        private IMinerInfoRepository minerInfoRepository;

        protected override string LogCategory => "Aion Payout Handler";

        #region IPayoutHandler

        public void ConfigureAsync(ClusterConfig clusterConfig, PoolConfig poolConfig)
        {
            this.poolConfig = poolConfig;
            this.clusterConfig = clusterConfig;
            extraConfig = poolConfig.PaymentProcessing.Extra.SafeExtensionDataAs<AionPoolPaymentExtraConfig>();

            logger = LogUtil.GetPoolScopedLogger(typeof(AionPayoutHandler), poolConfig);

            // configure standard daemon
            var jsonSerializerSettings = ctx.Resolve<JsonSerializerSettings>();

            var daemonEndpoints = poolConfig.Daemons
                .Where(x => string.IsNullOrEmpty(x.Category))
                .ToArray();

            daemon = new DaemonClient(jsonSerializerSettings, messageBus, clusterConfig.ClusterName ?? poolConfig.PoolName, poolConfig.Id);
            daemon.Configure(daemonEndpoints);
            rewardsCalculator = new AionRewardsCalculator();
        }

        public async Task<Block[]> ClassifyBlocksAsync(Block[] blocks)
        {
            Contract.RequiresNonNull(poolConfig, nameof(poolConfig));
            Contract.RequiresNonNull(blocks, nameof(blocks));

            var pageSize = 100;
            var pageCount = (int)Math.Ceiling(blocks.Length / (double)pageSize);
            var blockCache = new Dictionary<long, DaemonResponses.Block>();
            var result = new List<Block>();

            for (var i = 0; i < pageCount; i++)
            {
                // get a page full of blocks
                var page = blocks
                    .Skip(i * pageSize)
                    .Take(pageSize)
                    .ToArray();

                // get latest block
                var latestBlockResponses = await daemon.ExecuteCmdAllAsync<DaemonResponses.Block>(logger, AionCommands.GetBlockByNumber, new[] { (object)"latest", true });
                if (!latestBlockResponses.Any(x => x.Error == null && x.Response?.Height != null))
                    break;
                var latestBlockHeight = latestBlockResponses.First(x => x.Error == null && x.Response?.Height != null).Response.Height.Value;

                // execute batch
                var blockInfos = await FetchBlocks(blockCache, page.Select(block => (long)block.BlockHeight).ToArray());

                for (var j = 0; j < blockInfos.Length; j++)
                {
                    var blockInfo = blockInfos[j];
                    var block = page[j];

                    // extract confirmation data from stored block
                    var mixHash = block.TransactionConfirmationData.Split(":").First();
                    var nonce = block.TransactionConfirmationData.Split(":").Last();

                    // update progress
                    block.ConfirmationProgress = Math.Min(1.0d, (double)(latestBlockHeight - block.BlockHeight) / extraConfig.MinimumConfirmations);
                    result.Add(block);

                    // is it block mined by us?
                    if (blockInfo.Miner == poolConfig.Address)
                    {
                        // mature?
                        if (latestBlockHeight - block.BlockHeight >= (ulong) extraConfig.MinimumConfirmations)
                        {
                            block.Status = BlockStatus.Confirmed;
                            block.ConfirmationProgress = 1;

                            if ((long)block.BlockHeight < poolConfig.SignatureSwapProtocolUpgradeBlock) {
                                block.Reward = rewardsCalculator.calculateReward((long) block.BlockHeight);
                            } else {

                                long[] blockCacheHeights = new long[] {(long) block.BlockHeight, (long) block.BlockHeight - 1};

                                var cacheMisses = blockCacheHeights.Where(x => !blockCache.ContainsKey(x)).ToArray();
                                if (cacheMisses.Any()) {
                                    var blockBatch = cacheMisses.Select(height => new DaemonCmd(AionCommands.GetBlockByNumber,
                                        new[]
                                        {
                                            (object) height.ToStringHexWithPrefix(),
                                            true
                                        })).ToArray();

                                    var tmp = await daemon.ExecuteBatchAnyAsync(logger, blockBatch);

                                    var transformed = tmp
                                        .Where(x => x.Error == null && x.Response != null)
                                        .Select(x => x.Response?.ToObject<DaemonResponses.Block>())
                                        .Where(x => x != null)
                                        .ToArray();

                                    foreach (var b in transformed)
                                        blockCache[(long)b.Height.Value] = b;
                                }
                                           
                                block.Reward = rewardsCalculator.calculateRewardWithTimeSpan((long)(blockCache[(long) block.BlockHeight].Timestamp - blockCache[(long) block.BlockHeight - 1].Timestamp));
                            }

                            if (extraConfig?.KeepTransactionFees == false && blockInfo.Transactions?.Length > 0)
                                block.Reward += await GetTxRewardAsync(blockInfo); // tx fees

                            logger.Info(() => $"[{LogCategory}] Unlocked block {block.BlockHeight} worth {FormatAmount(block.Reward)}");
                        }

                        continue;
                    }

                    if (block.Status == BlockStatus.Pending && block.ConfirmationProgress > 0.75)
                    {
                        // we've lost this one
                        block.Status = BlockStatus.Orphaned;
                        block.Reward = 0;
                    }
                }
            }

            return result.ToArray();
        }

        public Task CalculateBlockEffortAsync(Block block, double accumulatedBlockShareDiff)
        {
            block.Effort = accumulatedBlockShareDiff / block.NetworkDifficulty;

            return Task.FromResult(true);
        }

        public override async Task<decimal> UpdateBlockRewardBalancesAsync(IDbConnection con, IDbTransaction tx, Block block, PoolConfig pool)
        {
            var blockRewardRemaining = await base.UpdateBlockRewardBalancesAsync(con, tx, block, pool);

            // Deduct static reserve for tx fees
            blockRewardRemaining -= (decimal) extraConfig.NrgFee;

            return blockRewardRemaining;
        }

        public async Task PayoutAsync(Balance[] balances)
        {
            // ensure we have peers
            var infoResponse = await daemon.ExecuteCmdSingleAsync<object>(logger, AionCommands.GetPeerCount);

            //TODO @AP-137 fix the validation
#if !DEBUG
            if (infoResponse.Error != null || 
                (Convert.ToInt32(infoResponse.Response)) < extraConfig.MinimumPeerCount)
            {
                logger.Warn(() => $"[{LogCategory}] Payout aborted. Not enough peers (" +
                extraConfig.MinimumPeerCount + " required)");
                return;
            }
#endif

            var txHashes = new List<string>();

            foreach (var balance in balances)
            {
                Miningcore.Persistence.Model.MinerInfo miner = await cf.RunTx(async (con, tx) =>
                {
                    return await minerInfoRepository.GetMinerInfo(con, tx, poolConfig.Id, balance.Address);
                });

                if (miner != null && miner.MinimumPayment > balance.Amount && extraConfig.EnableMinerMinimumPayment == true)
                    continue;

                try
                {
                    var txHash = await PayoutAsync(balance);
                    txHashes.Add(txHash);
                }

                catch (Exception ex)
                {
                    logger.Error(ex);

                    NotifyPayoutFailure(poolConfig.Id, new[] { balance }, ex.Message, null);
                }
            }

            if (txHashes.Any())
                NotifyPayoutSuccess(poolConfig.Id, balances, txHashes.ToArray(), null);
        }

        #endregion // IPayoutHandler

        private async Task<DaemonResponses.Block[]> FetchBlocks(Dictionary<long, DaemonResponses.Block> blockCache, params long[] blockHeights)
        {
            var cacheMisses = blockHeights.Where(x => !blockCache.ContainsKey(x)).ToArray();

            if (cacheMisses.Any())
            {
                var blockBatch = cacheMisses.Select(height => new DaemonCmd(AionCommands.GetBlockByNumber,
                    new[]
                    {
                        (object) height.ToStringHexWithPrefix(),
                        true
                    })).ToArray();

                var tmp = await daemon.ExecuteBatchAnyAsync(logger, blockBatch);

                var transformed = tmp
                    .Where(x => x.Error == null && x.Response != null)
                    .Select(x => x.Response?.ToObject<DaemonResponses.Block>())
                    .Where(x => x != null)
                    .ToArray();

                foreach (var block in transformed)
                    blockCache[(long)block.Height.Value] = block;
            }

            return blockHeights.Select(x => blockCache[x]).ToArray();
        }

        private async Task<decimal> GetTxRewardAsync(DaemonResponses.Block blockInfo)
        {
            // fetch all tx receipts in a single RPC batch request
            var batch = blockInfo.Transactions.Select(tx => new DaemonCmd(AionCommands.GetTxReceipt, new[] { tx.Hash }))
                .ToArray();

            var results = await daemon.ExecuteBatchAnyAsync(logger, batch);

            if (results.Any(x => x.Error != null))
                throw new Exception($"Error fetching tx receipts: {string.Join(", ", results.Where(x => x.Error != null).Select(y => y.Error.Message))}");

            // create lookup table
            var gasUsed = results.Select(x => x.Response.ToObject<TransactionReceipt>())
                .ToDictionary(x => x.TransactionHash, x => x.GasUsed);

            // accumulate
            var result = blockInfo.Transactions.Sum(x => (ulong)gasUsed[x.Hash] * ((decimal)x.GasPrice / AionConstants.Wei));

            return result;
        }

        private async Task<string> PayoutAsync(Balance balance)
        {
            DaemonResponse<string> response = null;
            
            if (extraConfig.SendTransactionsUsingPrivateKey.Equals(true)) 
            {
                response = await SendTransactionPrivateKey(balance);
            } else if (!String.IsNullOrEmpty(extraConfig.AccountPassword))
            {
                await UnlockAccount();
                response = await SendTransactionUnlockedAccount(balance);
            } else 
            {
                logger.Error(() => $"[{LogCategory}] The password or private key is missing from the configuration, unable to send payments.");
                throw new Exception("Missing password or private key");
            }

            if (response.Error != null)
                throw new Exception($"{AionCommands.SendTx} returned error: {response.Error.Message}, code {response.Error.Code}. {response.Error.Data}");

            if (string.IsNullOrEmpty(response.Response) || AionConstants.ZeroHashPattern.IsMatch(response.Response))
                throw new Exception($"{AionCommands.SendTx} did not return a valid transaction hash");

            var txHash = response.Response;
            logger.Info(() => $"[{LogCategory}] Payout transaction id: {txHash}");

            // update db
            await PersistPaymentsAsync(new[] { balance }, txHash);
            NotifyPayoutSuccess(poolConfig.Id, new[] { balance }, new[] { txHash }, null);
            
            // done
            return txHash;
        }

        private async Task<DaemonResponse<string>> SendTransactionPrivateKey(Balance balance)
        {
            logger.Info(() => $"[{LogCategory}] Sending {FormatAmount(balance.Amount)} to {balance.Address} using the private key.");
            var latestNonce = await getLatestNonce();
            var txData = new AionTransaction
            {
                Nonce = AionUtils.AppendHexStart(latestNonce),
                To = balance.Address,
                Value = AionUtils.AppendHexStart(((BigInteger)Math.Floor(balance.Amount * AionConstants.Wei)).ToString("x")),
                Data = "",
                Gas = AionUtils.AppendHexStart(new BigInteger(22000).ToString("x")),
                GasPrice = AionUtils.AppendHexStart(new BigInteger(10000000000).ToString("x")),
                Timestamp = AionUtils.AppendHexStart(DateTime.Now.Ticks.ToString("x2")),
                Type = "0x01"
            };
            txData.Sign(extraConfig.PrivateKey);
            string serializedTx = txData.Serialize();

            return await daemon.ExecuteCmdSingleAsync<string>(logger, AionCommands.SendRawTx, new[] { serializedTx });
        }

        private async Task UnlockAccount()
        {
            var unlockResponse = await daemon.ExecuteCmdSingleAsync<object>(logger, AionCommands.UnlockAccount, new[]
            {
                poolConfig.Address,
                extraConfig.AccountPassword,
                null
            });

            if (unlockResponse.Error != null || unlockResponse.Response == null || (bool)unlockResponse.Response == false)
                throw new Exception("Unable to unlock account for sending transaction");
        }

        private async Task<DaemonResponse<string>> SendTransactionUnlockedAccount(Balance balance) {
            var request = new SendTransactionRequest
            {
                To = balance.Address,
                From = poolConfig.Address,
                Value = (BigInteger)Math.Floor(balance.Amount * AionConstants.Wei),

            };
            logger.Info(() => $"[{LogCategory}] Sending {FormatAmount(balance.Amount)} to {balance.Address} using an unlocked account.");

            return await daemon.ExecuteCmdSingleAsync<string>(logger, AionCommands.SendTx, new[] { request });
        }

        private async Task<string> getLatestNonce() {
            var latest = await daemon.ExecuteCmdSingleAsync<string>(logger, AionCommands.GetTransactionCount, new[] { poolConfig.Address });
            if(AionUtils.fromHex(latest.Response).Equals(latestNonce) || usedLatestNonce) {
                latestNonce = latestNonce + 1;
                usedLatestNonce = false;
            } else {
                latestNonce = AionUtils.fromHex(latest.Response);
                usedLatestNonce = true;
            }

            return AionUtils.AppendHexStart(latestNonce.ToString("X"));
        }
    }
}
