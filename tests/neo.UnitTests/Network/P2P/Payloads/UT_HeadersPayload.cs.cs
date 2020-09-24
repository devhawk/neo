using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Models;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_HeadersPayload
    {
        [TestMethod]
        public void Size_Get()
        {
            var header = new Header(ProtocolSettings.Default.Magic);
            TestUtils.SetupHeaderWithValues(header, UInt256.Zero, out UInt256 merkRoot, out UInt160 val160, out ulong timestampVal, out uint indexVal, out Witness scriptVal);

            var test = HeadersPayload.Create();
            test.Size.Should().Be(1);
            test = HeadersPayload.Create(header);
            test.Size.Should().Be(1 + header.Size);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var header = new Header(ProtocolSettings.Default.Magic);
            TestUtils.SetupHeaderWithValues(header, UInt256.Zero, out UInt256 merkRoot, out UInt160 val160, out ulong timestampVal, out uint indexVal, out Witness scriptVal);
            var test = HeadersPayload.Create(header);
            var clone = test.ToArray().AsSerializable<HeadersPayload>();

            Assert.AreEqual(test.Headers.Length, clone.Headers.Length);
            Assert.AreEqual(test.Headers[0], clone.Headers[0]);
        }
    }
}
