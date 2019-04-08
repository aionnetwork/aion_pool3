using System;

namespace Miningcore.Persistence.Model
{
    public class MinerInfo
    {
        public string PoolId { get; set; }
        public string Miner { get; set; }
        public decimal MinimumPayment { get; set; }
    }
}
