﻿using System.Buffers.Binary;
using System.Text;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Messages.Client
{
    public class ClientCutText : IVNCMessage
    {
        public ProcessStatus Handle(MessageEvent ev)
        {
            // Message-Type (1) + Padding (3) + Length (4) + ?*1 >= 8
            if (ev.Data.Length < 8)
                return ProcessStatus.Invalid;

            if (ev.Data[0] != 6) // Message Type 6
                return ProcessStatus.Invalid;

            //TODO: padding check?
            // 3 bytes padding
            var length = BinaryPrimitives.ReadUInt32BigEndian(ev.Data[4..]);
            var end = 8 + length;
            if (ev.Data.Length != end)
                return ProcessStatus.Invalid;

            var text = Encoding.Default.GetString(ev.Data[8..]);
            ev.Log($"ClientCutText: Text ({text})");
            return ProcessStatus.Handled;
        }
    }
}
