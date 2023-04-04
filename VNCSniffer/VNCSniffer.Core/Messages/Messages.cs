using System.Net;
using VNCSniffer.Core.Messages.Client;
using VNCSniffer.Core.Messages.Handshake;
using VNCSniffer.Core.Messages.Handshake.SecurityTypes;
using VNCSniffer.Core.Messages.Initialization;
using VNCSniffer.Core.Messages.Server;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Messages
{
    public enum State
    {
        Unknown,
        // Handshakes
        ProtocolHandshakeS,
        ProtocolHandshakeC,
        SecurityHandshakeS,
        SecurityHandshakeC,
        SecurityVNCAuthChallenge,
        SecurityVNCAuthResponse,
        SecurityResult,
        // Initialization
        ClientInit,
        ServerInit,
        // Messages
        Initialized
    }

    public static class Messages
    {
        public class MessageEvent
        {
            public Participant Source;
            public Participant Destination;
            public Connection Connection;
            private byte[] DataArray;
            public ReadOnlySpan<byte> Data => DataArray;

            public MessageEvent(Participant source, Participant destination, Connection connection, byte[] data)
            {
                Source = source;
                Destination = destination;
                Connection = connection;
                DataArray = data;
            }

            public void SetData(byte[] data)
            {
                DataArray = data;
            }

            public void Log(string text) => Connection.LogData(Source, Destination, text);
        }

        public enum ProcessStatus
        {
            Invalid,
            NeedsMoreBytes,
            Handled
        }

        //TODO: instead use attributes?
        public static readonly Dictionary<State, IVNCMessage> Handlers = new()
        {
            { State.Unknown, new NotImplementedMessage("State.Unknown") },
            { State.ProtocolHandshakeS, new ServerProtocolHandshake() },
            { State.ProtocolHandshakeC, new ClientProtocolHandshake() },
            { State.SecurityHandshakeS, new ServerSecurityHandshake() },
            { State.SecurityHandshakeC, new ClientSecurityHandshake() },
            { State.SecurityVNCAuthChallenge, new VNCAuthChallenge() },
            { State.SecurityVNCAuthResponse, new VNCAuthResponse() },
            { State.SecurityResult, new SecurityResult() },
            { State.ClientInit, new ClientInit() },
            { State.ServerInit, new ServerInit() },
        };

        // Client To Server Messages
        //TODO: use attributes for this list and message type checks?
        public static readonly List<IVNCMessage> ClientHandlers = new()
        {
            { new SetPixelFormat() },
            { new SetEncodings() },
            { new FramebufferUpdateRequest() },
            { new KeyEvent() },
            { new PointerEvent() },
            { new ClientCutText() },
        };
        //TODO: SetClientServer in the messages

        // Server To Client Messages
        //TODO: use attributes for this list and message type checks?
        public static readonly List<IVNCMessage> ServerHandlers = new()
        {
            { new FramebufferUpdate() },
            { new SetColorMapEntries() },
            { new Bell() },
            { new ServerCutText() },
        };
    }

    public interface IVNCMessage
    {
        public abstract ProcessStatus Handle(MessageEvent e);
    }
}
