using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Messages.Server
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
