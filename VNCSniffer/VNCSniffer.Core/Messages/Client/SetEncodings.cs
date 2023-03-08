using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Messages.Client
{
    public class SetEncodings : IVNCMessage
    {
        public ProcessStatus Handle(MessageEvent ev)
        {
            // Message-Type (1) + Padding (1) + NumberOfEncodings (2) + ?*4 >= 4
            if (ev.Data.Length < 4)
                return ProcessStatus.Invalid;

            if (ev.Data[0] != 2) // Message Type 2
                return ProcessStatus.Invalid;

            //TODO: padding check?
            // 1 byte padding
            var numberOfEncodings = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[2..]);
            var end = 4 + numberOfEncodings * 4;
            if (ev.Data.Length != end)
                return ProcessStatus.Invalid;

            var encodings = new List<int>();
            for (var i = 0; i < numberOfEncodings; i++)
            {
                var index = 4 + i * 4;
                //INFO: encodings are signed
                var encoding = BinaryPrimitives.ReadInt32BigEndian(ev.Data[index..]);
                encodings.Add(encoding);
            }
            ev.Log($"SetEncodings: {string.Join(" ", encodings)}");
            return ProcessStatus.Handled;
        }
    }
}
