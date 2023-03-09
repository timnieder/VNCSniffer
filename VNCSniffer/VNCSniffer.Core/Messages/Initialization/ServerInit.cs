using System.Buffers.Binary;
using System.Text;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Messages.Initialization
{
    public class ServerInit : IVNCMessage
    {
        public ProcessStatus Handle(MessageEvent ev)
        {
            // Length: 2 + 2 + 16 + 4 + x >= 24
            if (ev.Data.Length < 24)
                return ProcessStatus.Invalid;

            ushort width = BinaryPrimitives.ReadUInt16BigEndian(ev.Data);
            ushort height = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[2..]);
            var format = new PixelFormat(ev.Data[4..20]);
            var nameLength = BinaryPrimitives.ReadUInt32BigEndian(ev.Data[20..]);
            var end = 24 + nameLength;
            if (ev.Data.Length != end)
                return ProcessStatus.Invalid;

            var name = Encoding.Default.GetString(ev.Data[24..]);

            ev.Connection.SetClientServer(ev.Destination, ev.DestinationPort, ev.Source, ev.SourcePort);
            ev.Connection.Width = width;
            ev.Connection.Height = height;
            ev.Connection.Format = format;

            ev.Log($"ServerInit: Width ({width}), Height ({height}), Name ({name})");
            return ProcessStatus.Handled;
        }
    }
}
