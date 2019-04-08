using System;

namespace Miningcore.Persistence.Model
{
    public class MinerWorkerPerformanceStats
    {
        public string PoolId { get; set; }
        public string Miner { get; set; }
        public string Worker { get; set; }
        public double Hashrate { get; set; }
        public double TotalPaid { get; set; }
        public double Efficiency { get; set; }
        public double SharesPerSecond { get; set; }
        public DateTime Created { get; set; }
    }
}
