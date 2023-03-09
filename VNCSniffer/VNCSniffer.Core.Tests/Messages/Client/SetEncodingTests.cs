using VNCSniffer.Core.Messages;
using VNCSniffer.Core.Messages.Client;

namespace VNCSniffer.Core.Tests.Messages.Client
{
    [TestClass]
    public class SetEncodingTests : BaseClientMessageTests
    {
        public override IVNCMessage Message => new SetEncodings();

        public override string FilePath => "clientSetEncodings.packet";
    }
}
