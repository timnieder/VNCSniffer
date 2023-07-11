using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Tests.Messages.Client
{
    [TestClass]
    public abstract class BaseClientMessageTests : BaseMessageTests
    {
        [TestMethod]
        public virtual void TestBasic()
        {
            Setup();

            Event.SetData(Data);
            var handled = Message.Handle(Event);
            Assert.AreEqual(ProcessStatus.Handled, handled);
        }

        [TestMethod]
        public virtual void TestInvalid()
        {
            // Setup
            Setup();

            // Test from every byte, except the last one
            for (var i = 0; i < Data.Length - 1; i++)
            {
                Event.SetData(Data[..i]);
                var handled = Message.Handle(Event);
                Assert.AreEqual(ProcessStatus.Invalid, handled);
            }
        }
    }
}
