using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VNCSniffer.Cli.Messages.Messages;

namespace VNCSniffer.Cli.Encodings
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
            index += length;
            return ProcessStatus.Handled;
        }
    }
}
