using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Messages.Handshake.SecurityTypes
{
    public class VNCAuthChallenge : IVNCMessage
    {
        public ProcessStatus Handle(MessageEvent ev)
        {
            if (ev.Data.Length != 16)
                return ProcessStatus.Invalid;

            var unsure = "?"; // cant be sure if this is the challenge
            if (ev.Connection.Client != null) // if we know where we are (we've got messages beforehand), we can be sure
                unsure = "";
            ev.Connection.Challenge = ev.Data.ToArray(); //TODO: better thing than copy?
            ev.Log($"Challenge{unsure}: {BitConverter.ToString(ev.Connection.Challenge)}");
            return ProcessStatus.Handled;
        }
    }

    public class VNCAuthResponse : IVNCMessage
    {
        public ProcessStatus Handle(MessageEvent ev)
        {
            if (ev.Data.Length != 16)
                return ProcessStatus.Invalid;

            if (ev.Connection.Challenge == null) // shouldn't happen?
            {
                ev.Connection.Challenge = ev.Data.ToArray();
                return ProcessStatus.Handled;
            }

            ev.Connection.ChallengeResponse = ev.Data.ToArray(); //TODO: better thing than copy?
            ev.Connection.SetClientServer(ev.Source, ev.Destination); // sent by client
            ev.Log($"Response: {BitConverter.ToString(ev.Connection.ChallengeResponse)}");
            return ProcessStatus.Handled;
        }
    }
}
