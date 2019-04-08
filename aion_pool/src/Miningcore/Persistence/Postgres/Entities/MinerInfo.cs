using System;

namespace Miningcore.Persistence.Postgres.Entities
{
    public class MinerInfo
    {
        public string PoolId { get; set; }
        public string Miner { get; set; }
        public decimal MinimumPayment { get; set; }
    }
}
