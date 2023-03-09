using VNCSniffer.Core.Messages;
using VNCSniffer.Core.Messages.Initialization;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Tests.Messages.Initialization
{
    [TestClass]
    public class ClientInitTests : BaseMessageTests
    {
        public override IVNCMessage Message => new ClientInit();

        public override string FilePath => "clientInit.packet";

        [TestMethod]
        public void TestBasic()
        {
            Setup();

            Event.SetData(Data);
            var handled = Message.Handle(Event);
            Assert.AreEqual(ProcessStatus.Handled, handled);
        }
    }
}
