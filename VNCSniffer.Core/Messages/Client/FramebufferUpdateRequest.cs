using System.Buffers.Binary;
using VNCSniffer.Core.Messages.Server;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Messages.Client
{
    public class FramebufferUpdateRequest : IVNCMessage
    {
        public ProcessStatus Handle(MessageEvent ev)
        {
            // Message-Type (1) + Incremental (1) + X (2) + Y (2) + W (2) + H (2) = 10
            if (ev.Data.Length != 10)
                return ProcessStatus.Invalid;

            if (ev.Data[0] != 3) // Message Type 3
                return ProcessStatus.Invalid;

            var incremental = Convert.ToBoolean(ev.Data[1]);
            var x = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[2..]);
            var y = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[4..]);
            var w = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[6..]);
            var h = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[8..]);

            // Check if the framebufferupdaterequest is bigger than our current size => resize
            FramebufferUpdate.CheckFramebufferSize(ev.Connection, x + w, y + h);

            ev.Log($"FramebufferUpdateRequest: Incremental ({incremental}), X ({x}), Y ({y}), W ({w}), H ({h})");
            return ProcessStatus.Handled;
        }
    }
}
