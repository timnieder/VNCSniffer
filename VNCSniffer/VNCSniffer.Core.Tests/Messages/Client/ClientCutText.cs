using VNCSniffer.Core.Messages;
using VNCSniffer.Core.Messages.Client;

namespace VNCSniffer.Core.Tests.Messages.Client
{
    [TestClass]
    public class ClientCutTextTests : BaseClientMessageTests
    {
        public override IVNCMessage Message => new ClientCutText();

        public override string FilePath => "clientClientCutText.packet";
    }
}
