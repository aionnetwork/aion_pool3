using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Miningcore.Extensions;

namespace Miningcore.Blockchain.Aion
{
	public class AionExtraNonceProvider: ExtraNonceProviderBase
    {
		public AionExtraNonceProvider() : base(2)
		{
		}
    }
}