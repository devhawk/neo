using Neo.Cryptography;
using Neo.IO;
using Neo.Models;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.Network.P2P
{
    public static class Helper
    {
        public static byte[] GetHashData(this ISignable verifiable)
        {
            return verifiable.GetHashData(ProtocolSettings.Default.Magic);
        }

        public static UInt256 CalculateHash(this ISignable verifiable)
        {
            return new UInt256(Crypto.Hash256(verifiable.GetHashData(ProtocolSettings.Default.Magic)));
        }

        public static UInt256 CalculateHash(this ISignable verifiable, uint magic)
        {
            return new UInt256(Crypto.Hash256(verifiable.GetHashData(magic)));
        }

        public static UInt256 CalculateHash(this ConsensusData @this)
        {
            return new UInt256(Crypto.Hash256(@this.ToArray()));
        }
    }
}
