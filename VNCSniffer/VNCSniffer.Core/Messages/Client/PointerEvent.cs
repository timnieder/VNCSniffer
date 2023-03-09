using System.Buffers.Binary;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Messages.Client
{
    public class PointerEvent : IVNCMessage
    {
        public ProcessStatus Handle(MessageEvent ev)
        {
            // Message-Type (1) + Mask (1) + X (2) + Y (2) = 6
            if (ev.Data.Length != 6)
                return ProcessStatus.Invalid;

            if (ev.Data[0] != 5) // Message Type 5
                return ProcessStatus.Invalid;

            var mask = ev.Data[1];
            var x = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[2..]);
            var y = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[4..]);

            ev.Log($"PointerEvent: Mask {Convert.ToString(mask, 2)}, X ({x}), Y ({y})");
            return ProcessStatus.Handled;
        }
    }
}
