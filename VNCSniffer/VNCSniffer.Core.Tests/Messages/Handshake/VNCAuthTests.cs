using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNCSniffer.Core.Messages;
using VNCSniffer.Core.Messages.Handshake;
using VNCSniffer.Core.Messages.Handshake.SecurityTypes;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Tests.Messages.Handshake
{
    [TestClass]
    public class VNCAuthChallengeTests : BaseMessageTests
    {
        public override IVNCMessage Message => new VNCAuthChallenge();

        public override string FilePath => "vncChallenge.packet";

        [TestMethod]
        public void TestChallenge()
        {
            Setup();

            Event.SetData(Data);
            var handled = Message.Handle(Event);
            Assert.AreEqual(ProcessStatus.Handled, handled);
            Assert.IsNotNull(Connection.Challenge);
        }
    }

    [TestClass]
    public class VNCAuthResponseTests : BaseMessageTests
    {
        public override IVNCMessage Message => new VNCAuthResponse();

        public override string FilePath => "vncResponse.packet";

        [TestMethod]
        public void TestNoChallenge()
        {
            Setup();

            Event.SetData(Data);
            var handled = Message.Handle(Event);
            Assert.AreEqual(ProcessStatus.Handled, handled);
            Assert.IsNotNull(Connection.Challenge);
            //TODO: check challenge
        }

        [TestMethod]
        public void TestChallengeCached()
        {
            Setup();
            Connection.Challenge = Data;

            Event.SetData(Data);
            var handled = Message.Handle(Event);
            Assert.AreEqual(ProcessStatus.Handled, handled);
            Assert.IsNotNull(Connection.Challenge);
            Assert.IsNotNull(Connection.ChallengeResponse);
            Assert.AreEqual(Event.Source, Connection.Client);
            Assert.AreEqual(Event.Destination, Connection.Server);
        }
    }
}
