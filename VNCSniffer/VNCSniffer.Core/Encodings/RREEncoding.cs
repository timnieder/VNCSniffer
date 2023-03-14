using System.Buffers.Binary;
using VNCSniffer.Core.Messages.Server;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Encodings
{
    public class RREEncoding : IEncoding
    {
        public ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index)
        {
            var bpp = e.Connection.Format != null ? e.Connection.Format.BitsPerPixel : 32;
            bpp /= 8;
            var headerLength = 4 + bpp;
            if (e.Data.Length < index + headerLength)
                return ProcessStatus.NeedsMoreBytes;

            var numberOfSubrectangles = BinaryPrimitives.ReadInt32BigEndian(e.Data[index..]);
            var bgColor = e.Data[(index + 4)..(index + headerLength)]; //TODO: parse pixel
            // draw bg
            e.Connection.DrawSolidRect(bgColor, ev.x, ev.y, ev.w, ev.h);
            index += headerLength;
            for (var i = 0; i < numberOfSubrectangles; i++)
            {
                var length = bpp + 8; // bpp + 2 + 2 + 2 + 2
                if (e.Data.Length < index + length)
                    return ProcessStatus.NeedsMoreBytes;

                var subrectClr = e.Data[index..(index + bpp)]; //TODO: parse pixel
                var x = BinaryPrimitives.ReadUInt16BigEndian(e.Data[(index + bpp)..]);
                var y = BinaryPrimitives.ReadUInt16BigEndian(e.Data[(index + bpp + 2)..]);
                var w = BinaryPrimitives.ReadUInt16BigEndian(e.Data[(index + bpp + 4)..]);
                var h = BinaryPrimitives.ReadUInt16BigEndian(e.Data[(index + bpp + 6)..]);
                // write into bitmap
                var absoluteX = (ushort)(ev.x + x);
                var absoluteY = (ushort)(ev.y + y);
                e.Connection.DrawSolidRect(subrectClr, absoluteX, absoluteY, w, h);
                index += length;
            }
            return ProcessStatus.Handled;
        }
    }
}
