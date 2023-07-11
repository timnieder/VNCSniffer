using VNCSniffer.Core.Messages;
using VNCSniffer.Core.Messages.Client;

namespace VNCSniffer.Core.Tests.Messages.Client
{
    [TestClass]
    public class SetPixelFormatTests : BaseClientMessageTests
    {
        public override IVNCMessage Message => new SetPixelFormat();

        public override string FilePath => "clientSetPixelFormat.packet";

        [TestMethod]
        public override void TestBasic()
        {
            base.TestBasic();
            Assert.AreEqual(Format, Connection.Format);
        }
    }
}
