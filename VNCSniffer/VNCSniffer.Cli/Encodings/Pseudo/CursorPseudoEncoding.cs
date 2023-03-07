using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VNCSniffer.Cli.Messages.Messages;
using VNCSniffer.Cli.Messages.Server;

namespace VNCSniffer.Cli.Encodings.Pseudo
{
    // Defines the position and shape of the cursor, so it can be drawn locally
    public class CursorPseudoEncoding : IEncoding
    {
        public ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index)
        {
            var bpp = e.Connection.Format != null ? e.Connection.Format.BitsPerPixel : 32;
            bpp /= 8;
            var pixelLength = ev.w * ev.h * bpp;
            var bitmaskLength = ((ev.w + 7) / 8) * ev.h; // div(width+7,8)*height
            if (e.Data.Length < index + pixelLength + bitmaskLength)
                return ProcessStatus.NeedsMoreBytes;

            //TODO: parse bitmap
            index += pixelLength;
            //TODO: parse bitmask
            index += bitmaskLength;
            return ProcessStatus.Handled;
        }
    }
}
