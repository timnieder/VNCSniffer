using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNCSniffer.Core.Messages;
using VNCSniffer.Core.Messages.Client;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Tests.Messages.Client
{
    [TestClass]
    public class SetEncodingTests : BaseMessageTests
    {
        public override IVNCMessage Message => new SetEncodings();

        public override string FilePath => "clientSetEncodings.packet";

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
