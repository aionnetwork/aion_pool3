
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
    public class RelayInfo
    {
        public RelayInfo()
        {
        }

        [Flags]
        public enum WireFormat
        {
            Json = 1,
            ProtocolBuffers = 2
        }

        public const int WireFormatMask = 0xF;
    }
}