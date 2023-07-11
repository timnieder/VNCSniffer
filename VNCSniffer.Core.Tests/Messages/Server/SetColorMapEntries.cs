using VNCSniffer.Core.Messages;
using VNCSniffer.Core.Messages.Server;

namespace VNCSniffer.Core.Tests.Messages.Server
{
    [TestClass]
    public class SetColorMapEntriesTests : BaseServerMessageTests
    {
        public override IVNCMessage Message => new SetColorMapEntries();

        public override string FilePath => "serverSetColorMapEntries.packet";
    }
}
