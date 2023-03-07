using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VNCSniffer.Cli.Messages.Messages;
using VNCSniffer.Cli.Messages;
using System.Buffers.Binary;
using VNCSniffer.Cli.Messages.Server;
using VNCSniffer.Cli.Encodings.Pseudo;

namespace VNCSniffer.Cli.Encodings
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
            // Pseudo
            { -223, new DesktopSizePseudoEncoding() },
            { -239, new CursorPseudoEncoding() },
        };
    }

    public interface IEncoding
    {
        //TODO: they also need a framebuffer
        public abstract ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index);
    }
}
