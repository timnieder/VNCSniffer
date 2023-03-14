using System.Buffers.Binary;
using VNCSniffer.Core.Messages.Server;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Encodings
{
    public class CopyRectEncoding : IEncoding
    {
        public ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index)
        {
            var length = 4;
            if (e.Data.Length < index + length)
                return ProcessStatus.NeedsMoreBytes;

            var srcXPos = BinaryPrimitives.ReadUInt16BigEndian(e.Data[index..]);
            var srcYPos = BinaryPrimitives.ReadUInt16BigEndian(e.Data[(index + 2)..]);
            // copy from framebuffer into framebuffer
            e.Connection.CopyRegion(srcXPos, srcYPos, ev.w, ev.h, ev.x, ev.y);
            index += length;
            return ProcessStatus.Handled;
        }
    }
}
