using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VNCSniffer.Cli.Messages.Messages;

namespace VNCSniffer.Cli.Messages.Handshake
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
