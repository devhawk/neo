using Akka.Actor;
using Akka.Routing;
using Neo.Models;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.Ledger
{
    internal class TransactionRouter : UntypedActor
    {
        public class Task { public Transaction Transaction; public bool Relay; }

        private readonly IActorRef blockchain;

        public TransactionRouter(NeoSystem system)
        {
            this.blockchain = system.Blockchain;
        }

        protected override void OnReceive(object message)
        {
            if (!(message is Task task)) return;
            blockchain.Tell(new Blockchain.PreverifyCompleted
            {
                Transaction = task.Transaction,
                Result = task.Transaction.VerifyStateIndependent(),
                Relay = task.Relay
            }, Sender);
        }

        internal static Props Props(NeoSystem system)
        {
            return Akka.Actor.Props.Create(() => new TransactionRouter(system)).WithRouter(new SmallestMailboxPool(Environment.ProcessorCount));
        }
    }
}
