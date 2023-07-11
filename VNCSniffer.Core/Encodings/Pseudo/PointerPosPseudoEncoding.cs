using VNCSniffer.Core.Messages.Server;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Encodings.Pseudo
{
    public class PointerPosPseudoEncoding : IEncoding
    {
        public ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index)
        {
            //TODO: pointerpos
            // no content
            return ProcessStatus.Handled;
        }
    }
}
