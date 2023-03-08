using VNCSniffer.Core.Encodings;

namespace VNCSniffer.Core.Tests.Encodings
{
    [TestClass]
    public class TRLEEncodingTests : BaseEncodingTests
    {
        public override IEncoding Encoding => new TRLEEncoding();

        public override string FilePath => "trle.packet";
        public override ushort PacketW => 20;
        public override ushort PacketH => 300;
    }
}