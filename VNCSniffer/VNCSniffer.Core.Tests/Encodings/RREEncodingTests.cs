using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using VNCSniffer.Core.Encodings;
using VNCSniffer.Core.Messages.Server;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Tests.Encodings
{
    [TestClass]
    public class RREEncodingTests
    {
        public static readonly byte[] packet_bytes = new byte[]
        {
            0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00,
            0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x96, 0x00, 0x96, 0xff, 0xff, 0xff, 0x00,
            0x00, 0x96, 0x00, 0x96, 0x00, 0x96, 0x00, 0x96
        };
        public static readonly ushort PacketW = 300;
        public static readonly ushort PacketH = 300;

        public static readonly PixelFormat Format = new()
        {
            BitsPerPixel = 32,
            Depth = 24,
            BigEndian = false,
            TrueColor = true,
            RedMax = 255,
            GreenMax = 255,
            BlueMax = 255,
            RedShift = 16,
            GreenShift = 8,
            BlueShift = 0,
        };

        [TestMethod]
        public void TestValid()
        {
            var rre = new RREEncoding();
            var data = packet_bytes;
            var connection = new Connection();
            connection.Format = Format;
            var e = new MessageEvent(IPAddress.None, 0, IPAddress.None, 0, connection, data);
            var ev = new FramebufferUpdateEvent(0, 0, PacketW, PacketH);
            var index = 0;
            var handled = rre.Parse(e, ev, ref index);
            Assert.AreEqual(ProcessStatus.Handled, handled);
        }

        [TestMethod]
        public void TestNeedMoreBytes()
        {
            // Setup
            var rre = new RREEncoding();
            var connection = new Connection();
            connection.Format = Format;
            var e = new MessageEvent(IPAddress.None, 0, IPAddress.None, 0, connection, null);
            var ev = new FramebufferUpdateEvent(0, 0, PacketW, PacketH);

            // Test from every byte, except the last one
            var data = packet_bytes;
            for (var i = 0; i < packet_bytes.Length - 1; i++)
            {
                e.SetData(packet_bytes[..i]);
                var index = 0;
                var handled = rre.Parse(e, ev, ref index);
                Assert.AreEqual(ProcessStatus.NeedsMoreBytes, handled);
            }
        }
    }
}