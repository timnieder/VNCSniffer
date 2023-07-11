using VNCSniffer.Core.Messages;
using VNCSniffer.Core.Messages.Server;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Tests.Messages.Server
{
    [TestClass]
    public class FramebufferUpdateTests : BaseServerMessageTests
    {
        public override IVNCMessage Message => new FramebufferUpdate();

        public override string FilePath => "serverFramebufferUpdate.packet";

        //TODO: check framebuffer at the end
        //TODO: check message with multiple rectangles
        [TestMethod]
        public override void TestInvalid()
        {
            // Setup
            Setup();

            // Test from every byte till the encoding message
            //TODO: skip encoding, go to next rectangle
            for (var i = 0; i < 16; i++)
            {
                Event.SetData(Data[..i]);
                var handled = Message.Handle(Event);
                Assert.AreEqual(ProcessStatus.Invalid, handled);
            }
            // The rest should be tested by encoding classes
        }
    }
}
