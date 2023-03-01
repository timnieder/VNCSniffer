using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VNCSniffer.Cli.Messages.Messages;
using VNCSniffer.Cli.Messages;
using System.Buffers.Binary;

namespace VNCSniffer.Cli.Encodings
{
    public static class Encodings
    {
        //TODO: instead use attributes?
        public static readonly Dictionary<int, IEncoding> Handlers = new()
        {
            { 0, new RawEncoding() },
        };
    }

    public interface IEncoding
    {
        //TODO: they also need a framebuffer
        public abstract ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index);
    }

    //TODO: move encodings into own files
    public class RawEncoding : IEncoding
    {
        public int GetLength(int x, int y, int w, int h, PixelFormat? format)
        {
            var bpp = format != null ? format.BitsPerPixel : 32;
            return w * h * (bpp / 8);
        }

        public ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index)
        {
            var bpp = e.Connection.Format != null ? e.Connection.Format.BitsPerPixel : 32;
            var length = ev.w * ev.h * (bpp / 8);
            if (e.Data.Length < index + length)
                return ProcessStatus.NeedsMoreBytes;

            index += length;
            return ProcessStatus.Handled;
        }
    }

    public class CopyRect : IEncoding
    {
        public int GetLength(int x, int y, int w, int h, PixelFormat format)
        {
            return 4; // src-x and src-y-position
        }

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
