using Microsoft.VisualBasic;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Text;
using VNCSniffer.Cli.Encodings;

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

        //TODO: move messages into own classes and files?
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
                    ev.Connection.SetClientServer(ev.Source, ev.SourcePort, ev.Destination, ev.DestinationPort);
                }
                else //TODO: we shouldnt even hit this?
                {
                    Debug.Assert(false, "ProtocolVersion not set");
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
            if (ev.Data.Length == 1 + numberOfSecurityTypes)
            {
                var encodings = string.Join(" ", ev.Data[1..].ToArray()); //TODO: better thing than copy?
                ev.Connection.SetClientServer(ev.Destination, ev.DestinationPort, ev.Source, ev.SourcePort); // sent by server
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
            ev.Connection.SetClientServer(ev.Source, ev.SourcePort, ev.Destination, ev.DestinationPort); // sent by client
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
            ev.Connection.SetClientServer(ev.Destination, ev.DestinationPort, ev.Source, ev.SourcePort);
            ev.Connection.Width = width;
            ev.Connection.Height = height;
            ev.Connection.Format = format;
            ev.Log($"ServerInit: Width ({width}), Height ({height}), Name ({name})");
            return true;
        }

        // Client To Server Messages
        //TODO: use attributes for this list and message type checks?
        public static readonly List<Func<MessageEvent, ProcessStatus>> ClientHandlers = new()
        {
            { HandleClientSetPixelFormat },
            { HandleClientSetEncodings },
            { HandleClientFramebufferUpdateRequest },
            { HandleClientKeyEvent },
            { HandleClientPointerEvent },
            { HandleClientClientCutText },
        };
        //TODO: SetClientServer in the functions
        public static ProcessStatus HandleClientSetPixelFormat(MessageEvent ev)
        {
            // Message-Type (1) + Padding (3) + PixelFormat(16) = 20
            if (ev.Data.Length != 20)
                return ProcessStatus.Invalid;

            if (ev.Data[0] != 0) // Message Type 0
                return ProcessStatus.Invalid;

            //TODO: padding check?
            // 3 bytes padding
            var format = new PixelFormat(ev.Data[4..]);
            ev.Log("SetPixelFormat");
            return ProcessStatus.Handled;
        }

        public static ProcessStatus HandleClientSetEncodings(MessageEvent ev)
        {
            // Message-Type (1) + Padding (1) + NumberOfEncodings (2) + ?*4 >= 4
            if (ev.Data.Length < 4)
                return ProcessStatus.Invalid;

            if (ev.Data[0] != 2) // Message Type 2
                return ProcessStatus.Invalid;

            //TODO: padding check?
            // 1 byte padding
            var numberOfEncodings = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[2..]);
            var end = 4 + numberOfEncodings * 4;
            if (ev.Data.Length != end)
                return ProcessStatus.Invalid;

            var encodings = new List<int>();
            for (var i = 0; i < numberOfEncodings; i++)
            {
                var index = 4 + i * 4;
                //INFO: encodings are signed
                var encoding = BinaryPrimitives.ReadInt32BigEndian(ev.Data[index..]);
                encodings.Add(encoding);
            }
            ev.Log($"SetEncodings: {string.Join(" ", encodings)}");
            return ProcessStatus.Handled;
        }

        public static ProcessStatus HandleClientFramebufferUpdateRequest(MessageEvent ev)
        {
            // Message-Type (1) + Incremental (1) + X (2) + Y (2) + W (2) + H (2) = 10
            if (ev.Data.Length != 10)
                return ProcessStatus.Invalid;

            if (ev.Data[0] != 3) // Message Type 3
                return ProcessStatus.Invalid;

            var incremental = Convert.ToBoolean(ev.Data[1]);
            var x = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[2..]);
            var y = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[4..]);
            var w = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[6..]);
            var h = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[8..]);

            ev.Log($"FramebufferUpdateRequest: Incremental ({incremental}), X ({x}), Y ({y}), W ({w}), H ({h})");
            return ProcessStatus.Handled;
        }

        public static ProcessStatus HandleClientKeyEvent(MessageEvent ev)
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

        public static ProcessStatus HandleClientPointerEvent(MessageEvent ev)
        {
            // Message-Type (1) + Mask (1) + X (2) + Y (2) = 6
            if (ev.Data.Length != 6)
                return ProcessStatus.Invalid;

            if (ev.Data[0] != 5) // Message Type 5
                return ProcessStatus.Invalid;

            var mask = ev.Data[1];
            var x = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[2..]);
            var y = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[4..]);

            ev.Log($"PointerEvent: Mask {Convert.ToString(mask, 2)}, X ({x}), Y ({y})");
            return ProcessStatus.Handled;
        }

        public static ProcessStatus HandleClientClientCutText(MessageEvent ev)
        {
            // Message-Type (1) + Padding (3) + Length (4) + ?*1 >= 8
            if (ev.Data.Length < 8)
                return ProcessStatus.Invalid;

            if (ev.Data[0] != 6) // Message Type 6
                return ProcessStatus.Invalid;

            //TODO: padding check?
            // 3 bytes padding
            var length = BinaryPrimitives.ReadUInt32BigEndian(ev.Data[4..]);
            var end = 8 + length;
            if (ev.Data.Length != end)
                return ProcessStatus.Invalid;

            var text = Encoding.Default.GetString(ev.Data[8..]);
            ev.Log($"ClientCutText: Text ({text})");
            return ProcessStatus.Handled;
        }

        // Server To Client Messages
        //TODO: use attributes for this list and message type checks?
        public static readonly List<Func<MessageEvent, ProcessStatus>> ServerHandlers = new()
        {
            { HandleServerFramebufferUpdate },
            { HandleServerSetColorMapEntries },
            { HandleServerBell },
            { HandleServerServerCutText },
        };

        public class Rectangle //TODO: move this somewhere else
        {
            public ushort x;
            public ushort y;
            public ushort w;
            public ushort h;
            public int encoding;

            public override string ToString()
            {
                return $"Rectangle: X ({x}), Y ({y}), W ({w}), H ({h}), Encoding ({encoding})";
            }
        }

        public class FramebufferUpdateEvent
        {
            public ushort x;
            public ushort y;
            public ushort w;
            public ushort h;

            public FramebufferUpdateEvent(ushort x, ushort y, ushort w, ushort h)
            {
                this.x = x;
                this.y = y;
                this.w = w;
                this.h = h;
            }
        }
        public static ProcessStatus HandleServerFramebufferUpdate(MessageEvent ev)
        {
            // Message-Type (1) + Padding (1) + NumberOfRectangles (2) + ?*12 >= 4
            if (ev.Data.Length < 4)
                return ProcessStatus.Invalid;

            if (ev.Data[0] != 0) // Message Type 0
                return ProcessStatus.Invalid;

            //TODO: padding check?
            // 1 byte padding
            var numberOfRectangles = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[2..]);
            var end = 4 + numberOfRectangles * 12;
            if (ev.Data.Length < end) //INFO: < cause there should be pixeldata after the rectangle headers
                return ProcessStatus.Invalid;

            var index = 4;
            var rectangles = new List<Rectangle>();
            //TODO: save the current state (i and buffer index) if we need more data but have parsed smth already, so we dont double parse
            for (var i = 0; i < numberOfRectangles; i++)
            {
                if (ev.Data.Length <= index + 12) // header check
                    return ProcessStatus.NeedsMoreBytes;
                // Parse header
                var x = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[index..]);
                var y = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[(index + 2)..]);
                var w = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[(index + 4)..]);
                var h = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[(index + 6)..]);
                var encoding = BinaryPrimitives.ReadInt32BigEndian(ev.Data[(index + 8)..]);
                index += 12;
                if (Encodings.Encodings.Handlers.TryGetValue(encoding, out var enc))
                {
                    var e = new FramebufferUpdateEvent(x, y, w, h);
                    var status = enc.Parse(ev, e, ref index);
                    if (status == ProcessStatus.NeedsMoreBytes)
                        return ProcessStatus.NeedsMoreBytes;

                    // else it was handled
                }
                else
                {
                    Console.WriteLine($"Encoding {encoding} not supported"); //TODO: can we do anything else?
                    //TODO: break? fallthrough and assume length is 0?
                }
                rectangles.Add(new Rectangle() { x = x, y = y, w = w, h = h, encoding = encoding });
            }
            ev.Log($"FramebufferUpdate: Rectangles ({numberOfRectangles}): {string.Join(";", rectangles)}");
            return ProcessStatus.Handled;
        }

        public static ProcessStatus HandleServerSetColorMapEntries(MessageEvent ev)
        {
            // Message-Type (1) + Padding (1) + FirstColor (2) + NumberOfColors (2) + ?*6 >= 6
            if (ev.Data.Length < 6)
                return ProcessStatus.Invalid;

            if (ev.Data[0] != 1) // Message Type 1
                return ProcessStatus.Invalid;

            //TODO: padding check?
            // 1 byte padding
            var firstColor = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[2..]);
            var numberOfColors = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[2..]);
            var end = 6 + numberOfColors * 6;
            if (ev.Data.Length != end)
                return ProcessStatus.Invalid;

            var colors = new List<string>(); //TODO: color class
            for (var i = 0; i < numberOfColors; i++)
            {
                var index = 6 + i * 6;
                var red = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[index..]);
                var green = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[(index + 2)..]);
                var blue = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[(index + 4)..]);
                colors.Add($"({red},{green},{blue})"); //TODO: class this
            }
            ev.Log($"SetColorMapEntries: Index ({firstColor}), Colors ({numberOfColors}): {string.Join(",", colors)}");
            return ProcessStatus.Handled;
        }

        public static ProcessStatus HandleServerBell(MessageEvent ev)
        {
            // Message-Type (1) = 1
            if (ev.Data.Length != 1)
                return ProcessStatus.Invalid;

            if (ev.Data[0] != 2) // Message Type 2
                return ProcessStatus.Invalid;

            ev.Log("Bell");
            return ProcessStatus.Handled;
        }

        public static ProcessStatus HandleServerServerCutText(MessageEvent ev)
        {
            // Message-Type (1) + Padding (3) + Length (4) + ?*1 >= 8
            if (ev.Data.Length < 8)
                return ProcessStatus.Invalid;

            if (ev.Data[0] != 3) // Message Type 3
                return ProcessStatus.Invalid;

            //TODO: padding check?
            // 3 bytes padding
            var length = BinaryPrimitives.ReadUInt32BigEndian(ev.Data[4..]);
            var end = 8 + length;
            if (ev.Data.Length != end)
                return ProcessStatus.Invalid;

            var text = Encoding.Default.GetString(ev.Data[8..]);
            ev.Log($"ServerCutText: Text ({text})");
            return ProcessStatus.Handled;
        }
    }
}
