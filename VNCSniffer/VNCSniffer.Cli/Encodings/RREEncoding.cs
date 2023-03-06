using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNCSniffer.Cli.Messages.Server;
using static VNCSniffer.Cli.Messages.Messages;

namespace VNCSniffer.Cli.Encodings
{
    public class RREEncoding : IEncoding
    {
        public ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index)
        {
            var bpp = e.Connection.Format != null ? e.Connection.Format.BitsPerPixel : 32;
            var headerLength = 4 + bpp;
            if (e.Data.Length < index + headerLength)
                return ProcessStatus.NeedsMoreBytes;

            var numberOfSubrectangles = BinaryPrimitives.ReadInt32BigEndian(e.Data[index..]);
            // e.Data[(index + 4)..]
            //var bgColor = ; //TODO: parse pixel
            index += headerLength;
            for (var i = 0; i < numberOfSubrectangles; i++)
            {
                var length = bpp + 8; // bpp + 2 + 2 + 2 + 2
                if (e.Data.Length < index + length)
                    return ProcessStatus.NeedsMoreBytes;

                //var subrectClr = ; //TODO: parse pixel
                var x = e.Data[(index + bpp)..];
                var y = e.Data[(index + bpp + 2)..];
                var w = e.Data[(index + bpp + 4)..];
                var h = e.Data[(index + bpp + 6)..];
                //TODO: write into bitmap
                index += length;
            }
            return ProcessStatus.Handled;
        }
    }
}
