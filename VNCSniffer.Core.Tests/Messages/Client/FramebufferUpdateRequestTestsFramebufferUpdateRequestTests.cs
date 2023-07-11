using VNCSniffer.Core.Messages;
using VNCSniffer.Core.Messages.Client;

namespace VNCSniffer.Core.Tests.Messages.Client
{
    [TestClass]
    public class FramebufferUpdateRequestTests : BaseClientMessageTests
    {
        public override IVNCMessage Message => new FramebufferUpdateRequest();

        public override string FilePath => "clientFramebufferUpdateRequest.packet";
    }
}
