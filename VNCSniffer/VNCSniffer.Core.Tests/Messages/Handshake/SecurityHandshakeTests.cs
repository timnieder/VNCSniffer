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
    public class ServerSecurityHandshakeTests : BaseMessageTests
    {
        public override IVNCMessage Message => new ServerSecurityHandshake();

        public override string FilePath => "serverSecurity.packet";

        [TestMethod]
        public void TestBasic()
        {
            Setup();

            Event.SetData(Data);
            var handled = Message.Handle(Event);
            Assert.AreEqual(ProcessStatus.Handled, handled);
            Assert.AreEqual(Event.Source, Connection.Server);
            Assert.AreEqual(Event.Destination, Connection.Client);
            //TODO: check securitytypes
        }
    }

    [TestClass]
    public class ClientSecurityHandshakeTests : BaseMessageTests
    {
        public override IVNCMessage Message => new ClientSecurityHandshake();

        public override string FilePath => "clientSecurity.packet";

        [TestMethod]
        public void TestClientServerSet()
        {
            // Setup
            Setup();
            Connection.SetClientServer(Event.Source, Event.SourcePort, Event.Destination, Event.DestinationPort);

            Event.SetData(Data);
            var handled = Message.Handle(Event);
            Assert.AreEqual(ProcessStatus.Handled, handled);
            Assert.IsNotNull(Connection.Client);
            Assert.IsNotNull(Connection.Server);
            //TODO: check selected security type
        }

        [TestMethod]
        public void TestMissingClient()
        {
            // Setup
            Setup();
            Connection.SetClientServer(Event.Destination, Event.DestinationPort, Event.Source, Event.SourcePort);

            Event.SetData(Data);
            var handled = Message.Handle(Event);
            Assert.AreEqual(ProcessStatus.Invalid, handled);
        }

        [TestMethod]
        public void TestNoClientServerSet()
        {
            // Setup
            Setup();

            Event.SetData(Data);
            var handled = Message.Handle(Event);
            Assert.AreEqual(ProcessStatus.Handled, handled);
            //TODO: check selected security type
        }
    }
}
