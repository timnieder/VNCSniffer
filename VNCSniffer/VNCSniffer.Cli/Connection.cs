using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VNCSniffer.Cli
{
    public class Connection
    {
        public State LastState = State.Unknown;

        public string? ProtocolVersion;
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
    }
}
