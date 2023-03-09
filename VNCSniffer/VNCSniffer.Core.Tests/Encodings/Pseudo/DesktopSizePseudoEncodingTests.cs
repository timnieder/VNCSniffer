using VNCSniffer.Core.Encodings;
using VNCSniffer.Core.Encodings.Pseudo;

namespace VNCSniffer.Core.Tests.Encodings
{
    [TestClass]
    public class DesktopSizePseudoEncodingTests : BaseEncodingTests
    {
        public override IEncoding Encoding => new DesktopSizePseudoEncoding();

        public override string FilePath => "desktopSizePseudo.packet";
        public override ushort PacketW => 300;
        public override ushort PacketH => 300;

        //TODO: check framebuffersize after tests
        [TestMethod]
        public override void TestHandled()
        {
            base.TestHandled();
            Assert.AreEqual(PacketW, Connection.Width);
            Assert.AreEqual(PacketH, Connection.Height);
        }
    }
}