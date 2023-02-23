using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace VNCSniffer.Cli
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
            public IPAddress Destination;
            public Connection Connection;
            private byte[] DataArray;
            public ReadOnlySpan<byte> Data => DataArray;

            public MessageEvent(IPAddress source, IPAddress destination, Connection connection, byte[] data)
            {
                Source = source;
                Destination = destination;
                Connection = connection;
                DataArray = data;
            }

            public void Log(string text) => Connection.LogData(Source, Destination, text);
        }

        public static readonly Dictionary<State, Func<MessageEvent, bool>> Handlers = new()
        {
            { State.Unknown, (_) => throw new NotImplementedException() },
            { State.ProtocolHandshakeS, HandleProtocolHandshakeS },
            { State.ProtocolHandshakeC, HandleProtocolHandshakeC },
            { State.SecurityHandshakeS, HandleSecurityHandshakeS },
            { State.SecurityHandshakeC, HandleSecurityHandshakeC },
            { State.SecurityVNCAuthChallenge, HandleVNCAuthChallenge },
            { State.SecurityVNCAuthResponse, HandleVNCAuthResponse },
            { State.SecurityResult, HandleSecurityResult },
            { State.ClientInit, HandleClientInit },
            { State.ServerInit, HandleServerInit },
        };

        public static bool HandleProtocolHandshakeS(MessageEvent ev)
        {
            var str = Encoding.Default.GetString(ev.Data);
            if (str.StartsWith("RFB"))
            {
                // We can't say if this is the server or client message, so we cant set it yet
                ev.Connection.ProtocolVersion = str;
                ev.Log($"ProtocolVersion: {str.TrimEnd()}");
                return true;
            }
            return false;
        }

        public static bool HandleProtocolHandshakeC(MessageEvent ev)
        {
            var str = Encoding.Default.GetString(ev.Data);
            if (str.StartsWith("RFB"))
            {
                if (ev.Connection.ProtocolVersion != null)
                {
                    ev.Connection.SetClientServer(ev.Source, ev.Destination);
                }
                else //TODO: we shouldnt even hit this?
                {
                    Debug.Assert(true, "ProtocolVersion not set");
                    ev.Connection.ProtocolVersion = str;
                }
                ev.Log($"ProtocolVersion: {str.TrimEnd()}");
                return true;
            }
            return false;
        }

        public static bool HandleSecurityHandshakeS(MessageEvent ev)
        {
            if (ev.Data.Length < 1)
                return false;

            var numberOfSecurityTypes = ev.Data[0];
            if (ev.Data.Length == (1 + numberOfSecurityTypes))
            {
                var encodings = string.Join(" ", ev.Data[1..].ToArray()); //TODO: better thing than copy?
                ev.Connection.SetClientServer(ev.Destination, ev.Source); // sent by server
                ev.Log($"Security Types ({numberOfSecurityTypes}): {encodings}");
                return true;
            }
            return false;
        }

        public static bool HandleSecurityHandshakeC(MessageEvent ev)
        {
            if (ev.Data.Length != 1)
                return false;

            if (ev.Connection.Client != null && !ev.Connection.Client.Equals(ev.Source))
                return false; //TODO: shouldnt happen?

            var securityType = ev.Data[0];
            //TODO: check if security type is valid/was offered by server?
            ev.Log($"Selected Security Type: {securityType}");
            return true;
        }

        public static bool HandleVNCAuthChallenge(MessageEvent ev)
        {
            if (ev.Data.Length != 16)
                return false;

            var unsure = "?"; // cant be sure if this is the challenge
            if (ev.Connection.Client != null) // if we know where we are (we've got messages beforehand), we can be sure
                unsure = "";
            ev.Connection.Challenge = ev.Data.ToArray(); //TODO: better thing than copy?
            ev.Log($"Challenge{unsure}: {BitConverter.ToString(ev.Connection.Challenge)}");
            return true;
        }

        public static bool HandleVNCAuthResponse(MessageEvent ev)
        {
            if (ev.Data.Length != 16)
                return false;

            ev.Connection.ChallengeResponse = ev.Data.ToArray(); //TODO: better thing than copy?
            ev.Connection.SetClientServer(ev.Source, ev.Destination); // sent by client
            ev.Log($"Response: {BitConverter.ToString(ev.Connection.ChallengeResponse)}");
            return true;
        }

        public static bool HandleSecurityResult(MessageEvent ev)
        {
            if (ev.Data.Length != 4)
                return false;

            var result = BinaryPrimitives.ReadUInt32BigEndian(ev.Data);
            ev.Log($"SecurityResult: {result}");
            return true;
        }

        public static bool HandleClientInit(MessageEvent ev)
        {
            if (ev.Data.Length != 1)
                return false;

            bool sharedFlag = Convert.ToBoolean(ev.Data[0]);
            ev.Log($"ClientInit: Shared Flag ({sharedFlag})");
            return true;
        }

        public static bool HandleServerInit(MessageEvent ev)
        {
            // Length: 2 + 2 + 16 + 4 + x >= 24
            if (ev.Data.Length < 24)
                return false;

            ushort width = BinaryPrimitives.ReadUInt16BigEndian(ev.Data);
            ushort height = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[2..]);
            var format = new PixelFormat(ev.Data[4..20]);
            var nameLength = BinaryPrimitives.ReadUInt32BigEndian(ev.Data[20..]);
            var end = 24 + nameLength;
            if (ev.Data.Length != end)
                return false;
            var name = Encoding.Default.GetString(ev.Data[24..]);
            ev.Connection.SetClientServer(ev.Destination, ev.Source);
            ev.Log($"ServerInit: Width ({width}), Height ({height}), Name ({name})");
            return true;
        }
    }
}
