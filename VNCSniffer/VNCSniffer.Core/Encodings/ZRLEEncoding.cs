using Ionic.Zlib;
using System.Buffers.Binary;
using VNCSniffer.Core.Messages.Server;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Encodings
{
    public class ZRLEEncoding : IEncoding
    {
        public ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index)
        {
            var length = 4;
            if (e.Data.Length < index + length)
                return ProcessStatus.NeedsMoreBytes;

            var dataLength = BinaryPrimitives.ReadInt32BigEndian(e.Data[index..]);
            index += length;
            if (e.Data.Length < index + dataLength)
                return ProcessStatus.NeedsMoreBytes;

            var zlibData = e.Data[index..(index + dataLength)];
            //TODO: can we just do this? dont we need a zlib object?
            var data = ZlibStream.UncompressBuffer(zlibData.ToArray());
            index += dataLength;

            var zLibDataIndex = 0; // as we're working on a new buffer we need to have a new index
            return TRLEEncoding.Decode(data, e.Connection.Format, ev, ref zLibDataIndex, 64);
        }
    }
}
