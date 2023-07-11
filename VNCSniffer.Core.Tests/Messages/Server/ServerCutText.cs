using VNCSniffer.Core.Messages;
using VNCSniffer.Core.Messages.Server;

namespace VNCSniffer.Core.Tests.Messages.Server
{
    [TestClass]
    public class ServerCutTextTests : BaseServerMessageTests
    {
        public override IVNCMessage Message => new ServerCutText();

        public override string FilePath => "serverServerCutText.packet";
    }
}
