using System.Net;
using VNCSniffer.Core.Encodings;
using VNCSniffer.Core.Messages.Server;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Tests.Encodings
{
    public abstract class BaseEncodingTests
    {
        public abstract IEncoding Encoding { get; }
        public abstract string FilePath { get; }
        public virtual ushort PacketX { get; } = 0;
        public virtual ushort PacketY { get; } = 0;
        public abstract ushort PacketW { get; }
        public abstract ushort PacketH { get; }

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

        protected byte[] Data;
        protected Connection Connection;
        protected MessageEvent Event;
        protected FramebufferUpdateEvent UpdateEvent;

        protected virtual byte[] GetData(string filePath)
        {
            var path = Path.GetFullPath($"../../../Data/Encodings/{filePath}");
            using (var file = File.Open(path, FileMode.Open, FileAccess.Read))
            {
                using (var sr = new StreamReader(file))
                {
                    var content = sr.ReadToEnd();
                    return Convert.FromHexString(content);
                }
            }
        }

        protected virtual void Setup()
        {
            Data = GetData(FilePath);
            var previewData = Data;
            if (previewData.Length > 10)
                previewData = Data[..10];
            Console.WriteLine($"Data ({Data.Length} Bytes): {Convert.ToHexString(previewData)}...");
            Connection = new Connection
            {
                Format = Format
            };
            Event = new MessageEvent(new(IPAddress.None, 0), new(IPAddress.None, 0), Connection, null);
            UpdateEvent = new FramebufferUpdateEvent(PacketX, PacketY, PacketW, PacketH);
        }

        [TestMethod]
        public virtual void TestNeedMoreBytes()
        {
            // Setup
            Setup();

            // Test from every byte, except the last one
            for (var i = 0; i < Data.Length - 1; i++)
            {
                Event.SetData(Data[..i]);
                var index = 0;
                var handled = Encoding.Parse(Event, UpdateEvent, ref index);
                Assert.AreEqual(ProcessStatus.NeedsMoreBytes, handled);
            }
        }

        [TestMethod]
        public virtual void TestHandled()
        {
            // Setup
            Setup();

            Event.SetData(Data);
            var index = 0;
            var handled = Encoding.Parse(Event, UpdateEvent, ref index);
            Assert.AreEqual(ProcessStatus.Handled, handled);
        }
    }
}
