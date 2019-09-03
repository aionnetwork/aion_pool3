using System.Numerics;
using System.Linq;
using System;
using System.Globalization;

namespace Miningcore.Blockchain.Aion
{
    class AionUtils 
    {
        public static decimal calculateReward(long height, decimal initialSupply, decimal forkBlock = 3346000) {
            var magnitude = 1000000000000000000;
            if(height < forkBlock) {
                return calculateOldReward(height) / magnitude;
            }

            return calculateCurrentReward(height, forkBlock, calculateTotalSupply(initialSupply, forkBlock));
        }

        public static decimal calculateTotalSupply(decimal initialSupply, decimal forkBlock) {
            var ts = initialSupply;
            for(var i = 1; i <= forkBlock; i++) {
                ts += calculateReward(i, forkBlock, initialSupply);
            }

            return ts;
        }

        public static decimal calculateOldReward(long height) {
            decimal blockReward = 1497989283243310185;
            var rampUpLowerBound = 0;
            var rampUpUpperBound = 259200;
            var rampUpStartValue = 748994641621655092;
            var rampUpEndValue = blockReward;

            var delta = rampUpUpperBound - rampUpLowerBound;
            var m = (rampUpEndValue - rampUpStartValue) / delta;

            if (height <= rampUpUpperBound) {
                return (m * height) + rampUpStartValue;
            } else {
                return blockReward;
            }
        }

        public static decimal calculateCurrentReward(long height, decimal forkBlock, decimal initialSupply) {
            var startingBlockNum = forkBlock;
            var annum = 3110400;
            var interestBasePoint = 100;
            var magnitude = 1000000000000000000;
            var inflationRate = 10000 + interestBasePoint;
            var term = (int) ((height - startingBlockNum - 1) / annum + 1);
            var divider = 10000;
            BigInteger compound = BigInteger.Multiply(new BigInteger(divider), new BigInteger(initialSupply));
            BigInteger preCompound = compound;

            for (long i = 0; i < term; i++) {
                preCompound = compound;
                compound = preCompound * inflationRate / divider;
            }

            compound = compound - preCompound;

            return (decimal) BigInteger.Divide(BigInteger.Divide(compound, annum), divider) / magnitude;
        }

        public static string diffToTarget(double diff)
        {
            BigInteger targetNew = (BigInteger.One << 256);
            targetNew =  targetNew / new BigInteger(diff);
            byte[] tmp = new byte[32];

            byte[] bytes = targetNew.ToByteArray().Reverse().ToArray();
            if (bytes.Length == 32) {
                return ByteToHex(bytes);
            } else {
                int start = bytes[0] == 0 ? 1 : 0;
                int count = bytes.Length - start;
                if (count > 32) {
                    //bug
                } else {
                    Array.Copy(bytes, start, tmp, tmp.Length - count, count);
                }
            }
            
            return ByteToHex(tmp);
        }

        public static void SetBytes(byte[] value, byte[] output, int startIndex)
        {
            if(output.Length < startIndex + value.Length)
                throw new Exception("Out of bounds");
            
            for(int i = 0; i < value.Length; i++) {
                output[startIndex + i] = value[i];
            }
        }

        public static string ByteToHex(byte[] tmp)
        {
            char[] c = new char[tmp.Length * 2];

            byte b;

            for(int bx = 0, cx = 0; bx < tmp.Length; ++bx, ++cx) 
            {
                b = ((byte)(tmp[bx] >> 4));
                c[cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);

                b = ((byte)(tmp[bx] & 0x0F));
                c[++cx]=(char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);
            }

            return new string(c);
        }

        public static int fromHex(string hex) {
            return hex.StartsWith("0x") ? int.Parse(hex.Substring(2), NumberStyles.HexNumber) : int.Parse(hex, NumberStyles.HexNumber);
        }
        public static string AppendHexStart(string hex) {
            if(hex.StartsWith("0x")) {
                hex = hex.Substring(2);
            }

            if(hex.Length % 2 != 0) {
                hex = "0" + hex;
            }

            return "0x" + hex;
        }
    }
}