using static VNCSniffer.Core.Messages.Messages;
using VNCSniffer.Core.Messages.Server;
using System.Buffers.Binary;
using System.IO.Compression;
using Ionic.Zlib;

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
            //FIXME: Bad state (unknown compression method (0x00))

            return TRLEEncoding.Decode(data, e.Connection.Format, ev, ref index, 64);
        }
    }
}
