using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNCSniffer.Core.Messages;
using VNCSniffer.Core.Messages.Handshake;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Tests.Messages.Handshake
{
    [TestClass]
    public class SecurityResultTest : BaseMessageTests
    {
        public override IVNCMessage Message => new SecurityResult();

        public override string FilePath => "securityResultOK.packet";

        [TestMethod]
        public void TestOK()
        {
            Setup();

            Event.SetData(Data);
            var handled = Message.Handle(Event);
            Assert.AreEqual(ProcessStatus.Handled, handled);
            //TODO: check security result
        }
    }
}
