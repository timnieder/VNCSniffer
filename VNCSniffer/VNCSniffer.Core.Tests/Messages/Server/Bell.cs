using VNCSniffer.Core.Messages;
using VNCSniffer.Core.Messages.Server;

namespace VNCSniffer.Core.Tests.Messages.Server
{
    [TestClass]
    public class BellTests : BaseServerMessageTests
    {
        public override IVNCMessage Message => new Bell();

        public override string FilePath => "serverBell.packet";
    }
}
