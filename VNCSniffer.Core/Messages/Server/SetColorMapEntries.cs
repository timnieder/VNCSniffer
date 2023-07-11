using System.Buffers.Binary;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Messages.Server
{
    public class SetColorMapEntries : IVNCMessage
    {
        public ProcessStatus Handle(MessageEvent ev)
        {
            // Message-Type (1) + Padding (1) + FirstColor (2) + NumberOfColors (2) + ?*6 >= 6
            if (ev.Data.Length < 6)
                return ProcessStatus.Invalid;

            if (ev.Data[0] != 1) // Message Type 1
                return ProcessStatus.Invalid;

            //TODO: padding check?
            // 1 byte padding
            var firstColor = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[2..]);
            var numberOfColors = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[4..]);
            var end = 6 + numberOfColors * 6;
            if (ev.Data.Length != end)
                return ProcessStatus.Invalid;

            var colors = new List<string>(); //TODO: color class
            for (var i = 0; i < numberOfColors; i++)
            {
                var index = 6 + i * 6;
                var red = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[index..]);
                var green = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[(index + 2)..]);
                var blue = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[(index + 4)..]);
                colors.Add($"({red},{green},{blue})"); //TODO: class this
            }
            ev.Log($"SetColorMapEntries: Index ({firstColor}), Colors ({numberOfColors}): {string.Join(",", colors)}");
            return ProcessStatus.Handled;
        }
    }
}
