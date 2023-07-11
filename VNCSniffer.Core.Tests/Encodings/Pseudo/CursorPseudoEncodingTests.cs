using VNCSniffer.Core.Encodings;
using VNCSniffer.Core.Encodings.Pseudo;

namespace VNCSniffer.Core.Tests.Encodings
{
    [TestClass]
    public class CursorPseudoEncodingTests : BaseEncodingTests
    {
        public override IEncoding Encoding => new CursorPseudoEncoding();

        public override string FilePath => "cursorPseudo.packet";
        public override ushort PacketW => 16;
        public override ushort PacketH => 16;
    }
}