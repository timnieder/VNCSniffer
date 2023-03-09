using VNCSniffer.Core.Messages.Server;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Encodings.Pseudo
{
    public class DesktopSizePseudoEncoding : IEncoding
    {
        public ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index)
        {
            // no content
            //TODO: resize framebuffer. x+y ignored, w+h as the new size
            e.Connection.Width = ev.w;
            e.Connection.Height = ev.h;
            return ProcessStatus.Handled;
        }
    }
}
