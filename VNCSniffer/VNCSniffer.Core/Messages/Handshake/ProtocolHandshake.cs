using System.Text;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Messages.Handshake
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
                    ev.Connection.SetClientServer(ev.Source, ev.Destination);
                }
                else //TODO: we shouldnt even hit this? so only tests
                {
                    ev.Connection.ProtocolVersion = str;
                }
                ev.Log($"ProtocolVersion: {str.TrimEnd()}");
                return ProcessStatus.Handled;
            }
            return ProcessStatus.Invalid;
        }
    }
}
