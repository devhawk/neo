using Neo.Models;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;

namespace Neo.IO.Caching
{
    internal class RelayCache : FIFOCache<UInt256, ISignable>
    {
        public RelayCache(int max_capacity)
            : base(max_capacity)
        {
        }

        protected override UInt256 GetKeyForItem(ISignable item)
        {
            return item.CalculateHash();
        }
    }
}
