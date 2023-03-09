using System.Net;
using VNCSniffer.Core.Messages;

namespace VNCSniffer.Core
{
    public class Connection
    {
        public State LastState = State.Unknown;

        public IPAddress? Buffer1Address;
        public ushort? Buffer1Port;
        public byte[]? Buffer1;
        public byte[]? Buffer2;

        public string? ProtocolVersion;
        //TODO: change those into a class containg both ip and port (or use flows?)
        public IPAddress? Client;
        public ushort? ClientPort;
        public IPAddress? Server;
        public ushort? ServerPort;

        public ushort? Width;
        public ushort? Height;
        public PixelFormat? Format;

        public byte[]? Challenge;
        public byte[]? ChallengeResponse;

        public void LogData(IPAddress source, ushort sourcePort, IPAddress dest, ushort destPort, string text)
        {
            var sourcePrefix = "";
            var destPrefix = "";
            if (source.Equals(Client) && sourcePort.Equals(ClientPort))
            {
                sourcePrefix = "C";
                destPrefix = "S";
            }
            else if (source.Equals(Server) && destPort.Equals(ServerPort))
            {
                sourcePrefix = "S";
                destPrefix = "C";
            }
            Console.WriteLine($"[{sourcePrefix}]{source}:{sourcePort}->[{destPrefix}]{dest}:{destPort}: {text}");
        }

        public void SetClientServer(IPAddress client, ushort clientPort, IPAddress server, ushort serverPort)
        {
            if (Client != null) // don't overwrite
                return;

            Client = client;
            ClientPort = clientPort;
            Server = server;
            ServerPort = serverPort;
        }

        public byte[]? GetBuffer(IPAddress address, ushort port)
        {
            // Buffer not assigned yet
            if (Buffer1Address == null)
            {
                Buffer1Address = address;
                Buffer1Port = port;
                return Buffer1;
            }

            if (address.Equals(Buffer1Address) && port.Equals(Buffer1Port))
                return Buffer1;
            else
                return Buffer2;
        }

        public void SetBuffer(IPAddress address, ushort port, byte[]? buffer)
        {
            // Buffer not assigned yet
            if (Buffer1Address == null)
            {
                Buffer1Address = address;
                Buffer1Port = port;
                Buffer1 = buffer;
            }

            if (address.Equals(Buffer1Address) && port.Equals(Buffer1Port))
                Buffer1 = buffer;
            else
                Buffer2 = buffer;
        }
    }
}
