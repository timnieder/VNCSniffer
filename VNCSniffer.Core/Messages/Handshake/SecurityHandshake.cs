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
                //ev.Connection.SetClientServer(ev.Destination, ev.Source); // sent by server //INFO: this conflicts with pointerevents, so dont set
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

            // only check if client is identified
            if (ev.Connection.Client != null && !ev.Source.Matches(ev.Connection.Client))
                return ProcessStatus.Invalid; //TODO: shouldnt happen?

            var securityType = ev.Data[0];
            //TODO: check if security type is valid/was offered by server?
            ev.Log($"Selected Security Type: {securityType}");
            return ProcessStatus.Handled;
        }
    }
}
