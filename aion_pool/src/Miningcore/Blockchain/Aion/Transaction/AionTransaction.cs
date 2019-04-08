using System.Numerics;
using System;
using Miningcore.Serialization;
using Newtonsoft.Json;
using Nethereum.RLP;
using Miningcore.Crypto.Hashing.Algorithms;
using Miningcore.Crypto;
using Miningcore.Blockchain.Aion;
using Sodium;
using Miningcore.Extensions;

namespace Miningcore.Blockchain.Aion.Transaction
{
    public class AionTransaction
    {
        public string To { get; set; }
        public string Value { get; set; }
        public string Data { get; set; } = "";
        public string Gas { get; set; } = "";
        public string GasPrice { get; set; } = "";
        public string Timestamp { get; set; } = "";
        public string Nonce { get; set; } = "";
        public string Type { get; set; } = "";
        public byte[] Signature { get; set; }

        public byte[] GetRawHash() {
            IHashAlgorithm hasher = new Blake2b();
            Span<byte> rawHash = stackalloc byte[32];
            hasher.Digest(GetEncodedRaw(), rawHash);

            return rawHash.ToArray();
        }

        public void Sign(string privateKey) {
            var signedBytes = PublicKeyAuth.SignDetached(GetRawHash(), privateKey.HexToByteArray());
            byte[] publicKey = new byte[32];
            Array.Copy(privateKey.HexToByteArray(), 32, publicKey, 0, 32);
            var signatureLength = signedBytes.Length + publicKey.Length;
            byte[] signatureArray = new byte[signatureLength];
            AionUtils.SetBytes(publicKey, signatureArray, 0);
            AionUtils.SetBytes(signedBytes, signatureArray, publicKey.Length);
            Signature = signatureArray;
        }

        public string Serialize() {
            return "0x" + AionUtils.ByteToHex(RLP.EncodeList(
                RLP.EncodeElement(Nonce.HexToByteArray()),
                RLP.EncodeElement(To.HexToByteArray()),
                RLP.EncodeElement(Value.HexToByteArray()),
                RLP.EncodeElement(Data.HexToByteArray()),
                RLP.EncodeElement(Timestamp.HexToByteArray()),
                CustomRLP.EncodeLong(Gas),
                CustomRLP.EncodeLong(GasPrice),
                RLP.EncodeElement(Type.HexToByteArray()),
                RLP.EncodeElement(Signature)
            ));
        }

        private byte[] GetEncodedRaw() {
            return CustomRLP.EncodeList(
                RLP.EncodeElement(Nonce.HexToByteArray()),
                RLP.EncodeElement(To.HexToByteArray()),
                RLP.EncodeElement(Value.HexToByteArray()),
                RLP.EncodeElement(Data.HexToByteArray()),
                RLP.EncodeElement(Timestamp.HexToByteArray()),
                CustomRLP.EncodeLong(Gas),
                CustomRLP.EncodeLong(GasPrice),
                RLP.EncodeElement(Type.HexToByteArray())
            );
        }
    }
}
