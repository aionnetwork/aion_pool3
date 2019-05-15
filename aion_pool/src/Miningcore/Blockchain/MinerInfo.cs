using System;
using ProtoBuf;

namespace Miningcore.Blockchain
{
    [ProtoContract]
    public class MinerInfo
    {
        public MinerInfo(string PoolId, string Miner, decimal MinimumPayment) 
        {
            this.PoolId = PoolId;
            this.Miner = Miner;
            this.MinimumPayment = MinimumPayment;
        }

        public MinerInfo()
        {
        }
        
        [ProtoMember(1)]
        public string PoolId { get; set; }
        [ProtoMember(2)]
        public string Miner { get; set; }
        [ProtoMember(3)]
        public decimal MinimumPayment { get; set; }
    }
}
