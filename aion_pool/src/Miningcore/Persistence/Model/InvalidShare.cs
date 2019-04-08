using System;

namespace Miningcore.Persistence.Model
{
    public class InvalidShare
    {
        public string PoolId { get; set; }
        public string Miner { get; set; }
        public string Worker { get; set; }
        public DateTime Created { get; set; }
    }
}
