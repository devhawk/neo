using Neo.IO;
using Neo.IO.Caching;
using Neo.IO.Json;
using Neo.Models;
using Neo.Network.P2P.Payloads;
using System.IO;
using System.Linq;

namespace Neo.Ledger
{
    public class TrimmedBlock : ICloneable<TrimmedBlock>, ISerializable
    {
        public Header Header;
        public UInt256[] Hashes;
        public ConsensusData ConsensusData;

        public bool IsBlock => Hashes.Length > 0;
        public uint Version => Header.Version;
        public UInt256 PrevHash => Header.PrevHash;
        public UInt256 MerkleRoot => Header.MerkleRoot;
        public ulong Timestamp => Header.Timestamp;
        public uint Index => Header.Index;
        public UInt160 NextConsensus => Header.NextConsensus;
        public Witness Witness => Header.Witness;

        public Block GetBlock(DataCache<UInt256, TransactionState> cache)
        {
            return new Block
            {
                Header = new Header
                {
                    Version = Header.Version,
                    PrevHash = Header.PrevHash,
                    MerkleRoot = Header.MerkleRoot,
                    Timestamp = Header.Timestamp,
                    Index = Header.Index,
                    NextConsensus = Header.NextConsensus,
                    Witness = Header.Witness,
                },
                ConsensusData = ConsensusData,
                Transactions = Hashes.Skip(1).Select(p => cache[p].Transaction).ToArray()
            };
        }

        public  int Size => 
            Header.Size
            + Hashes.GetVarSize()           //Hashes
            + (ConsensusData?.Size ?? 0);   //ConsensusData

        TrimmedBlock ICloneable<TrimmedBlock>.Clone()
        {
            return new TrimmedBlock
            {
                Header = new Header
                {
                    Version = Header.Version,
                    PrevHash = Header.PrevHash,
                    MerkleRoot = Header.MerkleRoot,
                    Timestamp = Header.Timestamp,
                    Index = Header.Index,
                    NextConsensus = Header.NextConsensus,
                    Witness = Header.Witness,
                },
                Hashes = Hashes,
                ConsensusData = ConsensusData,
            };
        }

        public void Deserialize(BinaryReader reader)
        {
            Header = reader.ReadSerializable<Header>();
            Hashes = reader.ReadSerializableArray<UInt256>(Block.MaxContentsPerBlock);
            if (Hashes.Length > 0)
                ConsensusData = reader.ReadSerializable<ConsensusData>();
        }

        void ICloneable<TrimmedBlock>.FromReplica(TrimmedBlock replica)
        {
            Header.Version = replica.Header.Version;
            Header.PrevHash = replica.Header.PrevHash;
            Header.MerkleRoot = replica.Header.MerkleRoot;
            Header.Timestamp = replica.Header.Timestamp;
            Header.Index = replica.Header.Index;
            Header.NextConsensus = replica.Header.NextConsensus;
            Header.Witness = replica.Header.Witness;
            Hashes = replica.Hashes;
            ConsensusData = replica.ConsensusData;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Header);
            writer.Write(Hashes);
            if (Hashes.Length > 0)
                writer.Write(ConsensusData);
        }

        public JObject ToJson(uint magic, byte addressVersion)
        {
            JObject json = Header.ToJson(magic, addressVersion);
            json["consensusdata"] = ConsensusData?.ToJson();
            json["hashes"] = Hashes.Select(p => (JObject)p.ToString()).ToArray();
            return json;
        }
    }
}
