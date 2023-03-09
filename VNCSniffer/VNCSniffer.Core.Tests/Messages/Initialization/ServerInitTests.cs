using VNCSniffer.Core.Messages;
using VNCSniffer.Core.Messages.Initialization;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Tests.Messages.Initialization
{
    [TestClass]
    public class ServerInitTests : BaseMessageTests
    {
        public override IVNCMessage Message => new ServerInit();

        public override string FilePath => "serverInit.packet";

        [TestMethod]
        public void TestBasic()
        {
            Setup();

            Event.SetData(Data);
            var handled = Message.Handle(Event);
            Assert.AreEqual(ProcessStatus.Handled, handled);
            Assert.AreEqual(Format, Connection.Format);
            Assert.AreEqual((ushort)300, Connection.Width);
            Assert.AreEqual((ushort)300, Connection.Height);
            Assert.AreEqual(Event.Source, Connection.Server);
            Assert.AreEqual(Event.Destination, Connection.Client);
        }
    }
}
