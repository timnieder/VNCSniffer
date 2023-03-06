using Microsoft.VisualBasic;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Text;
using VNCSniffer.Cli.Encodings;
using VNCSniffer.Cli.Messages.Client;
using VNCSniffer.Cli.Messages.Handshake;
using VNCSniffer.Cli.Messages.Handshake.SecurityTypes;
using VNCSniffer.Cli.Messages.Initialization;
using VNCSniffer.Cli.Messages.Server;
using static VNCSniffer.Cli.Messages.Messages;

namespace VNCSniffer.Cli.Messages
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
            public IPAddress Source;
            public ushort SourcePort;
            public IPAddress Destination;
            public ushort DestinationPort;
            public Connection Connection;
            private byte[] DataArray;
            public ReadOnlySpan<byte> Data => DataArray;

            public MessageEvent(IPAddress source, ushort sourcePort, IPAddress destination, ushort destPort, Connection connection, byte[] data)
            {
                Source = source;
                SourcePort = sourcePort;
                Destination = destination;
                DestinationPort = destPort;
                Connection = connection;
                DataArray = data;
            }

            public void Log(string text) => Connection.LogData(Source, SourcePort, Destination, DestinationPort, text);
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
