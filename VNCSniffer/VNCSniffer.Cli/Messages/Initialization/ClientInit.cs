using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VNCSniffer.Cli.Messages.Messages;

namespace VNCSniffer.Cli.Messages.Initialization
{
    public class ClientInit : IVNCMessage
    {
        public ProcessStatus Handle(MessageEvent ev)
        {
            if (ev.Data.Length != 1)
                return ProcessStatus.Invalid;

            bool sharedFlag = Convert.ToBoolean(ev.Data[0]);
            ev.Log($"ClientInit: Shared Flag ({sharedFlag})");
            return ProcessStatus.Handled;
        }
    }
}
