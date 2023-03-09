using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Messages.Client
{
    public class SetPixelFormat : IVNCMessage
    {
        public ProcessStatus Handle(MessageEvent ev)
        {
            // Message-Type (1) + Padding (3) + PixelFormat(16) = 20
            if (ev.Data.Length != 20)
                return ProcessStatus.Invalid;

            if (ev.Data[0] != 0) // Message Type 0
                return ProcessStatus.Invalid;

            //TODO: padding check?
            // 3 bytes padding
            var format = new PixelFormat(ev.Data[4..]);
            ev.Connection.Format = format;
            ev.Log("SetPixelFormat");
            return ProcessStatus.Handled;
        }
    }
}
