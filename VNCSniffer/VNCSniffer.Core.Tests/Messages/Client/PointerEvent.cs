using VNCSniffer.Core.Messages;
using VNCSniffer.Core.Messages.Client;

namespace VNCSniffer.Core.Tests.Messages.Client
{
    [TestClass]
    public class PointerEventTests : BaseClientMessageTests
    {
        public override IVNCMessage Message => new PointerEvent();

        public override string FilePath => "clientPointerEvent.packet";
    }
}
