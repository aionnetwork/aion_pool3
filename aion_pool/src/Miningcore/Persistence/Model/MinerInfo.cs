using System;

namespace Miningcore.Persistence.Model
{
    public class MinerInfo
    {
        public MinerInfo(string PoolId, string Miner, decimal MinimumPayment) {
            this.PoolId = PoolId;
            this.Miner = Miner;
            this.MinimumPayment = MinimumPayment;
        }
        
        public string PoolId { get; set; }
        public string Miner { get; set; }
        public decimal MinimumPayment { get; set; }
    }
}
