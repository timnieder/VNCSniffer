using VNCSniffer.Core.Encodings;

namespace VNCSniffer.Core.Tests.Encodings
{
    [TestClass]
    public class RREEncodingTests : BaseEncodingTests
    {
        public override IEncoding Encoding => new RREEncoding();

        public override string FilePath => "rre.packet";
        public override ushort PacketW => 300;
        public override ushort PacketH => 300;
    }
}