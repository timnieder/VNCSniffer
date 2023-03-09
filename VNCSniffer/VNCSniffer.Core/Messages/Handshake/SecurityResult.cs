using System.Buffers.Binary;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Messages.Handshake
{
    public class SecurityResult : IVNCMessage
    {
        public ProcessStatus Handle(MessageEvent ev)
        {
            if (ev.Data.Length != 4)
                return ProcessStatus.Invalid;

            var result = BinaryPrimitives.ReadUInt32BigEndian(ev.Data);
            ev.Log($"SecurityResult: {result}");
            return ProcessStatus.Handled;
        }
    }
}
