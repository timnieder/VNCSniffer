using VNCSniffer.Core.Encodings.Pseudo;
using VNCSniffer.Core.Messages.Server;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Encodings
{
    public static class Encodings
    {
        //TODO: instead use attributes?
        public static readonly Dictionary<int, IEncoding> Handlers = new()
        {
            { 0, new RawEncoding() },
            { 1, new CopyRectEncoding() },
            { 2, new RREEncoding() },
            { 5, new HextileEncoding() },
            { 15, new TRLEEncoding() },
            { 16, new ZRLEEncoding() },
            // Extensions
            // Pseudo
            { -223, new DesktopSizePseudoEncoding() },
            { -239, new CursorPseudoEncoding() },
            // Pseudo Extensions
            { -232, new PointerPosPseudoEncoding() },
            { -308, new ExtendedDesktopSizePseudoEncoding() },
        };
    }

    public interface IEncoding
    {
        public abstract ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index);
    }
}
