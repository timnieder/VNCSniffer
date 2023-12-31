﻿using System.Buffers.Binary;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Messages.Client
{
    public class KeyEvent : IVNCMessage
    {
        public ProcessStatus Handle(MessageEvent ev)
        {
            // Message-Type (1) + Down (1) + Padding (2) + Key (4) = 8
            if (ev.Data.Length != 8)
                return ProcessStatus.Invalid;

            if (ev.Data[0] != 4) // Message Type 4
                return ProcessStatus.Invalid;

            //TODO: padding check?
            var downFlag = Convert.ToBoolean(ev.Data[1]);
            // 2 bytes padding
            var key = BinaryPrimitives.ReadUInt32BigEndian(ev.Data[4..]);
            ev.Log($"KeyEvent: Key {key}, Down: {downFlag}");
            return ProcessStatus.Handled;
        }
    }
}
