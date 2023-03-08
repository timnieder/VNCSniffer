using VNCSniffer.Core.Encodings;

namespace VNCSniffer.Core.Tests.Encodings
{
    [TestClass]
    public class HextileEncodingTests : BaseEncodingTests
    {
        public override IEncoding Encoding => new HextileEncoding();

        public override string FilePath => "hextile.packet";
        public override ushort PacketW => 20;
        public override ushort PacketH => 300;
    }
}