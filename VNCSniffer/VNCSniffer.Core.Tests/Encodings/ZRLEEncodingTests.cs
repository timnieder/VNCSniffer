using VNCSniffer.Core.Encodings;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Tests.Encodings
{
    [TestClass]
    public class ZRLEEncodingTests : BaseEncodingTests
    {
        public override IEncoding Encoding => new ZRLEEncoding();

        public override string FilePath => "zrle.packet";
        public override ushort PacketW => 300;
        public override ushort PacketH => 300;

        [TestMethod]
        public virtual void TestZlib()
        {
            // Setup
            Setup();

            Data = GetData("zrle1.packet");
            UpdateEvent.w = 800;
            UpdateEvent.h = 600;

            Event.SetData(Data);
            var index = 0;
            var handled = Encoding.Parse(Event, UpdateEvent, ref index);
            Assert.AreEqual(ProcessStatus.Handled, handled);
        }
        [TestMethod]
        public virtual void TestDeflate()
        {
            // Setup
            Setup();

            Data = GetData("zrle2.packet");
            UpdateEvent.w = 800;
            UpdateEvent.h = 600;

            Event.SetData(Data);
            var index = 0;
            var handled = Encoding.Parse(Event, UpdateEvent, ref index);
            Assert.AreEqual(ProcessStatus.Handled, handled);
        }
    } //TODO: test mid connection ZRLE packages
}