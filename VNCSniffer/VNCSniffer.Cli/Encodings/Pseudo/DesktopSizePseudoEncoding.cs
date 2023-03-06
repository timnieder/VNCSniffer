using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNCSniffer.Cli.Messages.Server;
using static VNCSniffer.Cli.Messages.Messages;

namespace VNCSniffer.Cli.Encodings.Pseudo
{
    public class DesktopSizePseudoEncoding : IEncoding
    {
        public ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index)
        {
            // no content
            //TODO: resize framebuffer. x+y ignored, w+h as the new size
            return ProcessStatus.Handled;
        }
    }
}
