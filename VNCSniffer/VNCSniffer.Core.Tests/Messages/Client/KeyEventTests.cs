using VNCSniffer.Core.Messages;
using VNCSniffer.Core.Messages.Client;

namespace VNCSniffer.Core.Tests.Messages.Client
{
    [TestClass]
    public class KeyEventTests : BaseClientMessageTests
    {
        public override IVNCMessage Message => new KeyEvent();

        public override string FilePath => "clientKeyEvent.packet";
    }
}
