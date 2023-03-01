using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VNCSniffer.Cli.Messages.Messages;
using VNCSniffer.Cli.Messages;

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
        //TODO: as the length is kinda dynamic we probably need to do a parse func,
        // which return a processstatus? some encodings know the length from the static params,
        // but some read from the data (ZRLE, RRE, ...)
        //TODO: GetData func

        //TODO: do we need a GetLength func?
        //TODO: instead give the connection as a param? depends on what the encodings need
        public abstract int GetLength(int x, int y, int w, int h, PixelFormat format);
    }

    //TODO: move encodings into own files
    public class RawEncoding : IEncoding
    {
        public int GetLength(int x, int y, int w, int h, PixelFormat? format)
        {
            var bpp = format != null ? format.BitsPerPixel : 32;
            return w * h * (bpp / 8);
        }
    }

    public class CopyRect : IEncoding
    {
        public int GetLength(int x, int y, int w, int h, PixelFormat format)
        {
            return 4; // src-x and src-y-position
        }
    }
}
