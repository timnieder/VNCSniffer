using VNCSniffer.Core.Encodings;

namespace VNCSniffer.Core.Tests.Encodings
{
    [TestClass]
    public class ZRLEEncodingTests : BaseEncodingTests
    {
        public override IEncoding Encoding => new ZRLEEncoding();

        public override string FilePath => "zrle.packet";
        public override ushort PacketW => 300;
        public override ushort PacketH => 300;
    } //TODO: test mid connection ZRLE packages
}