using System;
using ProtoBuf;

namespace Miningcore.Blockchain
{
    [ProtoContract]
    public class InvalidShare: RelayInterface
    {
        /// <summary>
        /// The pool originating this share from
        /// </summary>
        [ProtoMember(1)]
        public string PoolId { get; set; }

        /// <summary>
        /// Who mined it (wallet address)
        /// </summary>
        [ProtoMember(2)]
        public string Miner { get; set; }

        /// <summary>
        /// Who mined it
        /// </summary>
        [ProtoMember(3)]
        public string Worker { get; set; }

        /// <summary>
        /// When the share was found
        /// </summary>
        [ProtoMember(4)]
        public DateTime Created { get; set; }
    }
}
