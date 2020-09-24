using Neo.Models;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.SmartContract
{
    public class LogEventArgs : EventArgs
    {
        public ISignable ScriptContainer { get; }
        public UInt160 ScriptHash { get; }
        public string Message { get; }

        public LogEventArgs(ISignable container, UInt160 script_hash, string message)
        {
            this.ScriptContainer = container;
            this.ScriptHash = script_hash;
            this.Message = message;
        }
    }
}
