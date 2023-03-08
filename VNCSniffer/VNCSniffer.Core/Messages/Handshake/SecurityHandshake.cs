using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Messages.Handshake
{
    public class ServerSecurityHandshake : IVNCMessage
    {
        public ProcessStatus Handle(MessageEvent ev)
        {
            if (ev.Data.Length < 1)
                return ProcessStatus.Invalid;

            var numberOfSecurityTypes = ev.Data[0];
            if (ev.Data.Length == 1 + numberOfSecurityTypes)
            {
                var encodings = string.Join(" ", ev.Data[1..].ToArray()); //TODO: better thing than copy?
                ev.Connection.SetClientServer(ev.Destination, ev.DestinationPort, ev.Source, ev.SourcePort); // sent by server
                ev.Log($"Security Types ({numberOfSecurityTypes}): {encodings}");
                return ProcessStatus.Handled;
            }
            return ProcessStatus.Invalid;
        }
    }

    public class ClientSecurityHandshake : IVNCMessage
    {
        public ProcessStatus Handle(MessageEvent ev)
        {
            if (ev.Data.Length != 1)
                return ProcessStatus.Invalid;

            if (ev.Connection.Client != null && !ev.Connection.Client.Equals(ev.Source))
                return ProcessStatus.Invalid; //TODO: shouldnt happen?

            var securityType = ev.Data[0];
            //TODO: check if security type is valid/was offered by server?
            ev.Log($"Selected Security Type: {securityType}");
            return ProcessStatus.Handled;
        }
    }
}
