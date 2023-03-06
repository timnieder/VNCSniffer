using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VNCSniffer.Cli.Messages.Messages;

namespace VNCSniffer.Cli.Messages.Handshake
{
    public class ServerProtocolHandshake : IVNCMessage
    {
        public ProcessStatus Handle(MessageEvent ev)
        {
            var str = Encoding.Default.GetString(ev.Data);
            if (str.StartsWith("RFB"))
            {
                // We can't say if this is the server or client message, so we cant set it yet
                ev.Connection.ProtocolVersion = str;
                ev.Log($"ProtocolVersion: {str.TrimEnd()}");
                return ProcessStatus.Handled;
            }
            return ProcessStatus.Invalid;
        }
    }

    public class ClientProtocolHandshake : IVNCMessage
    {
        public ProcessStatus Handle(MessageEvent ev)
        {
            var str = Encoding.Default.GetString(ev.Data);
            if (str.StartsWith("RFB"))
            {
                if (ev.Connection.ProtocolVersion != null)
                {
                    ev.Connection.SetClientServer(ev.Source, ev.SourcePort, ev.Destination, ev.DestinationPort);
                }
                else //TODO: we shouldnt even hit this?
                {
                    Debug.Assert(false, "ProtocolVersion not set");
                    ev.Connection.ProtocolVersion = str;
                }
                ev.Log($"ProtocolVersion: {str.TrimEnd()}");
                return ProcessStatus.Handled;
            }
            return ProcessStatus.Invalid;
        }
    }
}
