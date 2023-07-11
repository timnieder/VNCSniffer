using VNCSniffer.Core.Encodings.Pseudo;
using VNCSniffer.Core.Messages.Server;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Encodings
{
    public static class Encodings
    {
        //TODO: instead use attributes?
        public static readonly Dictionary<Encoding, IEncoding> Handlers = new()
        {
            { Encoding.Raw, new RawEncoding() },
            { Encoding.CopyRect, new CopyRectEncoding() },
            { Encoding.RRE, new RREEncoding() },
            { Encoding.Hextile, new HextileEncoding() },
            { Encoding.TRLE, new TRLEEncoding() },
            { Encoding.ZRLE, new ZRLEEncoding() },
            // Extensions
            // Pseudo
            { Encoding.DesktopSize, new DesktopSizePseudoEncoding() },
            { Encoding.Cursor, new CursorPseudoEncoding() },
            // Pseudo Extensions
            { Encoding.PointerPos, new PointerPosPseudoEncoding() },
            { Encoding.ExtendedDesktopSize, new ExtendedDesktopSizePseudoEncoding() },
        };

        public enum Encoding
        {
            Raw = 0,
            CopyRect = 1,
            RRE = 2,
            Hextile = 5,
            TRLE = 15,
            ZRLE = 16,
            // Extensions

            // Pseudo
            DesktopSize = -223,
            Cursor = -239,
            // Extended Pseudo
            PointerPos = -232,
            ExtendedDesktopSize = -308,
        }
    }

    

    public interface IEncoding
    {
        public abstract ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index);
    }
}
