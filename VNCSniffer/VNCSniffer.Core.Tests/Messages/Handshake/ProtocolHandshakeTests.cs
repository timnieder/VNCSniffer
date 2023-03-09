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
    public class ServerProtocolHandshakeTests : BaseMessageTests
    {
        public override IVNCMessage Message => new ServerProtocolHandshake();

        public override string FilePath => "protocol.packet";

        [TestMethod]
        public void TestBasic()
        {
            Setup();

            Event.SetData(Data);
            var handled = Message.Handle(Event);
            Assert.AreEqual(ProcessStatus.Handled, handled);
            Assert.AreEqual("RFB 003.008\n", Connection.ProtocolVersion);
        }
    }

    [TestClass]
    public class ClientProtocolHandshakeTests : BaseMessageTests
    {
        public override IVNCMessage Message => new ClientProtocolHandshake();

        public override string FilePath => "protocol.packet";

        [TestMethod]
        public void TestAfterServer()
        {
            // Setup
            Setup();
            Connection.ProtocolVersion = "RFB 003.008\n";

            Event.SetData(Data);
            var handled = Message.Handle(Event);
            Assert.AreEqual(ProcessStatus.Handled, handled);
            Assert.AreEqual(Event.Source, Connection.Client);
            Assert.AreEqual(Event.Destination, Connection.Server);
        }

        [TestMethod]
        public void TestBasic()
        {
            Setup();

            Event.SetData(Data);
            var handled = Message.Handle(Event);
            Assert.AreEqual(ProcessStatus.Handled, handled);
            Assert.AreEqual("RFB 003.008\n", Connection.ProtocolVersion);
        }
    }
}
