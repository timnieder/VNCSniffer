using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNCSniffer.Cli.Messages.Server;
using static VNCSniffer.Cli.Messages.Messages;

namespace VNCSniffer.Cli.Encodings
{
    public class RawEncoding : IEncoding
    {
        public ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index)
        {
            var bpp = e.Connection.Format != null ? e.Connection.Format.BitsPerPixel : 32;
            var length = ev.w * ev.h * (bpp / 8);
            if (e.Data.Length < index + length)
                return ProcessStatus.NeedsMoreBytes;

            //TODO: parse bitmap
            index += length;
            return ProcessStatus.Handled;
        }
    }
}
