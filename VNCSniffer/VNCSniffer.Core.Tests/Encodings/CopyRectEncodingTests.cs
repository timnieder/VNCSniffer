using VNCSniffer.Core.Encodings;

namespace VNCSniffer.Core.Tests.Encodings
{
    [TestClass]
    public class CopyRectEncodingTests : BaseEncodingTests
    {
        public override IEncoding Encoding => new CopyRectEncoding();

        public override string FilePath => "copyrect.packet";
        public override ushort PacketW => 150;
        public override ushort PacketH => 150;
        public override ushort PacketX => 150;
        public override ushort PacketY => 0;
    }
}