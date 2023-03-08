using VNCSniffer.Core.Encodings;

namespace VNCSniffer.Core.Tests.Encodings
{
    [TestClass]
    public class RawEncodingTests : BaseEncodingTests
    {
        public override IEncoding Encoding => new RawEncoding();

        public override string FilePath => "raw.packet";
        public override ushort PacketW => 20;
        public override ushort PacketH => 20;
    }
}