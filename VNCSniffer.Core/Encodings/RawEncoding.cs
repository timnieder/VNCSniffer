using VNCSniffer.Core.Messages.Server;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Encodings
{
    public class RawEncoding : IEncoding
    {
        public ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index)
        {
            var format = e.Connection.PixelFormat;
            var bpp = format.BitsPerPixel;
            bpp /= 8;
            var length = ev.w * ev.h * bpp;
            if (e.Data.Length < index + length)
                return ProcessStatus.NeedsMoreBytes;

            // draw bitmap
            e.Connection.DrawRegion(e.Data[index..(index + length)], ev.x, ev.y, ev.w, ev.h);
            index += length;
            return ProcessStatus.Handled;
        }
    }
}
