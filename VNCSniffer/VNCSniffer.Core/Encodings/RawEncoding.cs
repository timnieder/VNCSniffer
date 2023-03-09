using VNCSniffer.Core.Messages.Server;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Encodings
{
    public class RawEncoding : IEncoding
    {
        public ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index)
        {
            var bpp = e.Connection.Format != null ? e.Connection.Format.BitsPerPixel : 32;
            bpp /= 8;
            var length = ev.w * ev.h * bpp;
            if (e.Data.Length < index + length)
                return ProcessStatus.NeedsMoreBytes;

            //TODO: parse bitmap
            index += length;
            return ProcessStatus.Handled;
        }
    }
}
