using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Models;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Oracle;
using Neo.VM;
using System;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public static class Verifiable 
    {
        public static bool Verify(this ISignable @this, StoreView snapshot)
        {
            if (@this is Transaction tx)
            {
                return Verify(tx, snapshot, null) == VerifyResult.Succeed;
            }

            if (@this is ConsensusPayload consensusPayload)
            {
                if (consensusPayload.BlockIndex <= snapshot.Height)
                    return false;
                return consensusPayload.VerifyWitnesses(snapshot, 0_02000000);
            }

            if (@this is Header header)
            {
                return VerifyHeader(header);
            }

            if (@this is Block block)
            {
                return VerifyHeader(block.Header);
            }

            throw new Exception("Invalid ISignable");

            bool VerifyHeader(Header header)
            {
                Header prev_header = snapshot.GetHeader(header.PrevHash);
                if (prev_header == null) return false;
                if (prev_header.Index + 1 != header.Index) return false;
                if (prev_header.Timestamp >= header.Timestamp) return false;
                if (!header.VerifyWitnesses(snapshot, 1_00000000)) return false;
                return true;
            }
        }

        public static VerifyResult VerifyStateDependent(this Transaction tx, StoreView snapshot, TransactionVerificationContext context)
        {
            if (tx.ValidUntilBlock <= snapshot.Height || tx.ValidUntilBlock > snapshot.Height + Transaction.MaxValidUntilBlockIncrement)
                return VerifyResult.Expired;
            UInt160[] hashes = tx.GetScriptHashesForVerifying(snapshot);
            if (NativeContract.Policy.IsAnyAccountBlocked(snapshot, hashes))
                return VerifyResult.PolicyFail;
            if (NativeContract.Policy.GetMaxBlockSystemFee(snapshot) < tx.SystemFee)
                return VerifyResult.PolicyFail;
            if (!(context?.CheckTransaction(tx, snapshot) ?? true)) return VerifyResult.InsufficientFunds;
            foreach (TransactionAttribute attribute in tx.Attributes)
                if (!attribute.Verify(snapshot, tx))
                    return VerifyResult.Invalid;
            long net_fee = tx.NetworkFee - tx.Size * NativeContract.Policy.GetFeePerByte(snapshot);
            if (!tx.VerifyWitnesses(snapshot, net_fee, WitnessFlag.StateDependent))
                return VerifyResult.Invalid;
            return VerifyResult.Succeed;
        }

        public static VerifyResult VerifyStateIndependent(this Transaction tx)
        {
            if (tx.Size > Transaction.MaxTransactionSize)
                return VerifyResult.Invalid;
            if (!tx.VerifyWitnesses(null, tx.NetworkFee, WitnessFlag.StateIndependent))
                return VerifyResult.Invalid;
            return VerifyResult.Succeed;
        }

        public static VerifyResult Verify(this Transaction tx, StoreView snapshot, TransactionVerificationContext context)
        {
            VerifyResult result = VerifyStateIndependent(tx);
            if (result != VerifyResult.Succeed) return result;
            result = VerifyStateDependent(tx, snapshot, context);
            return result;
        }

        public static UInt160[] GetScriptHashesForVerifying(this ISignable @this, StoreView snapshot)
        {
            if (@this is Transaction tx)
            {
                return tx.Signers.Select(p => p.Account).ToArray();
            }

            if (@this is ConsensusPayload consensusPayload)
            {
                ECPoint[] validators = NativeContract.NEO.GetNextBlockValidators(snapshot);
                if (validators.Length <= consensusPayload.ValidatorIndex)
                    throw new InvalidOperationException();
                return new[] { Contract.CreateSignatureRedeemScript(validators[consensusPayload.ValidatorIndex]).ToScriptHash() };
            }

            if (@this is Header header)
            {
                return GetHeaderScriptHashes(header);
            }

            if (@this is Block block)
            {
                return GetHeaderScriptHashes(block.Header);
            }

            throw new Exception("Invalid ISignable");
            
            UInt160[] GetHeaderScriptHashes(Header header)
            {
                if (header.PrevHash == UInt256.Zero) return new[] { header.Witness.ScriptHash };
                Header prev_header = snapshot.GetHeader(header.PrevHash);
                if (prev_header == null) throw new InvalidOperationException();
                return new[] { prev_header.NextConsensus };
            }
        }

        public static bool Verify(this TransactionAttribute @this, StoreView snapshot, Transaction tx)
        {
            if (@this is HighPriorityAttribute highPriority)
            {
                UInt160 committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
                return tx.Signers.Any(p => p.Account.Equals(committee));
            }

            if (@this is OracleResponse oracleResponse)
            {
                if (tx.Signers.Any(p => p.Scopes != WitnessScope.None)) return false;
                if (!tx.Script.AsSpan().SequenceEqual(fixedScript.Value)) return false;
                OracleRequest request = NativeContract.Oracle.GetRequest(snapshot, oracleResponse.Id);
                if (request is null) return false;
                if (tx.NetworkFee + tx.SystemFee != request.GasForResponse) return false;
                UInt160 oracleAccount = Blockchain.GetConsensusAddress(NativeContract.Oracle.GetOracleNodes(snapshot));
                return tx.Signers.Any(p => p.Account.Equals(oracleAccount));
            }

            throw new Exception("invalid TransactionAttribute");
        }

        readonly static Lazy<byte[]> fixedScript = new Lazy<byte[]>(() => 
        {
            using ScriptBuilder sb = new ScriptBuilder();
            sb.EmitAppCall(NativeContract.Oracle.Hash, "finish");
            return sb.ToArray();
        });

    }
}
