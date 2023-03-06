using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VNCSniffer.Cli.Messages.Messages;

namespace VNCSniffer.Cli.Messages.Server
{
    public class Bell : IVNCMessage
    {
        public ProcessStatus Handle(MessageEvent ev)
        {
            // Message-Type (1) = 1
            if (ev.Data.Length != 1)
                return ProcessStatus.Invalid;

            if (ev.Data[0] != 2) // Message Type 2
                return ProcessStatus.Invalid;

            ev.Log("Bell");
            return ProcessStatus.Handled;
        }
    }
}
