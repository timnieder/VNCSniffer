using VNCSniffer.Core.Messages.Server;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Encodings.Pseudo
{
    public class ExtendedDesktopSizePseudoEncoding : IEncoding
    {
        public ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index)
        {
            //TODO: extended desktopsize
            return ProcessStatus.Handled;
        }
    }
}
