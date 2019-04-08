using System.Numerics;
using System.Linq;
using System;
using System.Globalization;
using Nethereum.RLP;
using Miningcore.Extensions;

namespace Miningcore.Blockchain.Aion.Transaction
{
    class CustomRLP
    {
        public static byte[] EncodeLong(string number)
        {
            var numberWithoutX = number.StartsWith("0x") ? number.Substring(2) : number;
            var bigNumber = BigInteger.Parse(numberWithoutX, NumberStyles.AllowHexSpecifier);
            var sizeThreshold = BigInteger.Parse("00000000FFFFFFFF", NumberStyles.AllowHexSpecifier);
            if (bigNumber.CompareTo(sizeThreshold) < 0)
            {
                return RLP.EncodeElement(number.HexToByteArray());
            }

            return RLP.EncodeElement(PadTo16Bytes(bigNumber.ToString("x")).HexToByteArray());
        }

        public static byte[] EncodeList(params byte[][] input)
        {
            var buf = Combine(input);
            var combined = new byte[2][];
            combined[0] = EncodeLength(buf.Length, 192);
            combined[1] = buf;
            return Combine(combined);
        }

        public static byte[] EncodeLength(int len, int offset)
        {
            if (len < 56)
            {
                int result = len + offset;
                return BitConverter.GetBytes(result).Reverse().ToArray();
            }

            var hexLength = len.ToString("x");
            var lLength = hexLength.Length / 2;
            var firstByte = (offset + 55 + lLength).ToString("x");
            return (firstByte + hexLength).HexToByteArray();
        }

        public static string PadTo16Bytes(string hexNumber)
        {
            if (hexNumber.Length % 2 != 0)
            {
                hexNumber = "0" + hexNumber;
            }

            for (var i = 0; i < 16 - hexNumber.Length + 1; i++)
            {
                hexNumber = "0" + hexNumber;
            }

            return "0x00" + hexNumber;
        }

        public static byte[] Combine(params byte[][] arrays)
        {
            byte[] ret = new byte[arrays.Sum(x => x.Length)];
            int offset = 0;
            foreach (byte[] data in arrays)
            {
                Buffer.BlockCopy(data, 0, ret, offset, data.Length);
                offset += data.Length;
            }
            return ret;
        }
    }
}
