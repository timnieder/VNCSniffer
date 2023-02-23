using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VNCSniffer.Cli
{
    public enum State
    {
        Unknown,
        ProtocolHandshake,
        Handshake,
        Initialized
    }

    public class Connection
    {
        public State State = State.Unknown;

        public string? ProtocolVersion;
        public IPAddress? Client;
        public IPAddress? Server;

        public byte[]? Challenge;
        public byte[]? ChallengeResponse;

        public void LogData(IPAddress source, IPAddress dest, string text)
        {
            var sourcePrefix = "";
            var destPrefix = "";
            if (source.Equals(Client))
            {
                sourcePrefix = "C";
                destPrefix = "S";
            }
            else if (source.Equals(Server))
            {
                sourcePrefix = "S";
                destPrefix = "C";
            }
            Console.WriteLine($"[{sourcePrefix}]{source}->[{destPrefix}]{dest}: {text}");
        }

        public void SetClientServer(IPAddress client, IPAddress server) 
        {
            if (Client != null) // don't overwrite
                return;

            Client = client;
            Server = server;
        }
    }
}
