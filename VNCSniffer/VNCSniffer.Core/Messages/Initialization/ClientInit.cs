using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Messages.Initialization
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
