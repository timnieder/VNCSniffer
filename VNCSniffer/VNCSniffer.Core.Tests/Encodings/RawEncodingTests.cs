using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using VNCSniffer.Core.Encodings;
using VNCSniffer.Core.Messages.Server;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Tests.Encodings
{
    [TestClass]
    public class RawEncodingTests
    {
        public static string filePath = "raw.packet";
        public static readonly ushort PacketW = 20;
        public static readonly ushort PacketH = 20;

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

        private byte[] GetData()
        {
            var path = Path.GetFullPath($"../../../Data/{filePath}");
            using (var file = File.Open(path, FileMode.Open, FileAccess.Read))
            {
                using (var sr = new StreamReader(file))
                {
                    var content = sr.ReadToEnd();
                    return Convert.FromHexString(content);
                }
            }
        }
        [TestMethod]
        public void TestValid()
        {
            var data = GetData();
            Console.WriteLine($"Data ({data.Length} Bytes): {Convert.ToHexString(data[..10])}...");

            var encoding = new RawEncoding();
            var connection = new Connection();
            connection.Format = Format;
            var e = new MessageEvent(IPAddress.None, 0, IPAddress.None, 0, connection, data);
            var ev = new FramebufferUpdateEvent(0, 0, PacketW, PacketH);
            var index = 0;
            var handled = encoding.Parse(e, ev, ref index);
            Assert.AreEqual(ProcessStatus.Handled, handled);
        }

        [TestMethod]
        public void TestNeedMoreBytes()
        {
            // Setup
            var data = GetData();
            Console.WriteLine($"Data ({data.Length} Bytes): {Convert.ToHexString(data[..10])}...");
            var encoding = new RawEncoding();
            var connection = new Connection();
            connection.Format = Format;
            var e = new MessageEvent(IPAddress.None, 0, IPAddress.None, 0, connection, null);
            var ev = new FramebufferUpdateEvent(0, 0, PacketW, PacketH);

            // Test from every byte, except the last one
            for (var i = 0; i < data.Length - 1; i++)
            {
                e.SetData(data[..i]);
                var index = 0;
                var handled = encoding.Parse(e, ev, ref index);
                Assert.AreEqual(ProcessStatus.NeedsMoreBytes, handled);
            }
        }
    }
}