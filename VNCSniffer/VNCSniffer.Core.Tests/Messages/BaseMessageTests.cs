using System.Net;
using VNCSniffer.Core.Messages;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Tests.Messages
{
    public abstract class BaseMessageTests
    {
        public abstract IVNCMessage Message { get; }
        public abstract string FilePath { get; }

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

        protected virtual byte[] GetData(string filePath)
        {
            var path = Path.GetFullPath($"../../../Data/Messages/{filePath}");
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
            var source = new IPAddress(new byte[] { 192, 168, 0, 1 });
            ushort sourcePort = 59404;
            var dest = new IPAddress(new byte[] { 192, 168, 0, 5 });
            ushort destPort = 5900;
            Event = new MessageEvent(source, sourcePort, dest, destPort, Connection, null);
        }
    }
}
