using PacketDotNet;
using PacketDotNet.Connections;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Diagnostics;
using System.Net;

namespace VNCSniffer.Cli
{
    public class Cli
    {
        static Dictionary<TcpConnection, Connection> Connections = new();
        private static TcpConnectionManager tcpConnectionManager = new();
        public static void Main(string[] args)
        {
            Debug.AutoFlush = true;
            Debug.WriteLine("Start");

            string? path = null;
            if (args.Length > 0)
            {
                path = args[0];

                if (!File.Exists(path))
                {
                    Console.WriteLine($"File {path} doesn't exist.");
                }
            }

            ICaptureDevice device;
            if (path != null)
            {
                device = new CaptureFileReaderDevice(path);
            }
            else
            {
                var devices = CaptureDeviceList.Instance;
                if (devices.Count == 0)
                {
                    Console.WriteLine("No interfaces found! Make sure WinPcap/Npcap/libpcap is installed.");
                    return;
                }

                //TODO: select via config
                device = devices.FirstOrDefault(x => x.Description.Contains("loopback"));
                if (device == null)
                {
                    Console.WriteLine("Unable to find configured device.");
                    return;
                }
            }
            Console.WriteLine($"Using device {device.Description} ({device.Name})");

            tcpConnectionManager.OnConnectionFound += TcpConnectionManager_OnConnectionFound;

            //TODO: create better filter? also let user select port?
            device.Open(DeviceModes.Promiscuous); //TODO: read timeout ms
            var filter = "tcp and " +
                    "(port 5900 or " +
                    "port 5901)";
            device.Filter = filter;
            //device.OnPacketArrival += PacketHandler;
            device.OnPacketArrival += Device_OnPacketArrival;
            Console.WriteLine("Listening...");
            device.Capture();
            device.Close();
        }

        private static void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            var rawPacket = e.GetPacket();
            var p = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

            var tcpPacket = p.Extract<TcpPacket>();

            if (tcpPacket != null)
            {
                // Pass the packet to the connection manager
                tcpConnectionManager.ProcessPacket(rawPacket.Timeval, tcpPacket);
            }
        }

        private static void TcpConnectionManager_OnConnectionFound(TcpConnection c)
        {
            if (!Connections.TryGetValue(c, out _))
                Connections.Add(c, new());

            c.OnPacketReceived += PacketHandler;
        }

        private static void PacketHandler(PosixTimeval timeval, TcpConnection tcpConnection, TcpFlow flow, TcpPacket tcp)
        {
            if (!tcp.HasPayloadData)
                return;

            if (!tcp.Push) //TODO: check if they only push
                return;

            //TODO: handle multi packet msgs?
            var msg = tcp.PayloadData;
            if (msg == null || msg.Length == 0)
                return;

            var ip = (IPPacket)tcp.ParentPacket;
            var source = ip.SourceAddress;
            var sourcePort = tcp.SourcePort;
            var dest = ip.DestinationAddress;
            var destPort = tcp.DestinationPort;

            if (!Connections.TryGetValue(tcpConnection, out var connection))
            {
                Debug.Assert(connection != null, "No connection found for tcp connection.");
                return;
            }

            var parsed = ParseMessage(connection, source, sourcePort, dest, destPort, msg);
            if (parsed)
                return;

            var data = BitConverter.ToString(msg);
            if (data.Length > 50)
                data = data.Substring(0, 50) + "...";
            Console.WriteLine($"[{tcp.SequenceNumber}] {source}:{sourcePort}->{dest}:{destPort}: {data}");
        }

        private static bool ParseMessage(Connection connection, IPAddress source, ushort sourcePort, IPAddress dest, ushort destPort, byte[] data)
        {
            var result = false;
            // Our connection isnt initialized yet (or so we think)
            // Therefore we check from the laststate if any messages can be parsed
            var ev = new Messages.MessageEvent(source, dest, connection, data);
            var ev = new Messages.MessageEvent(source, sourcePort, dest, destPort, connection, data);
            if (connection.LastState < State.Initialized)
            {
                for (var i = connection.LastState + 1; i < State.Initialized; i++)
                {
                    var handled = Messages.Handlers[i](ev);
                    if (handled)
                    {
                        connection.LastState = i;
                        return true;
                    }
                }
                //TODO: if we hit the end without a valid parsed msgs, are we inited?
                connection.LastState = State.Initialized;
            }

            // parse c2s/s2c messages
            var checkClientMsgs = true;
            var checkServerMsgs = true;
            if (source.Equals(connection.Client) && sourcePort.Equals(connection.ClientPort))
            {
                // source is the client => only check client msgs
                checkServerMsgs = false;
            }
            else if (source.Equals(connection.Server) && sourcePort.Equals(connection.ServerPort))
            {
                // source is the server => only check server msgs
                checkClientMsgs = false;
            }

            if (checkClientMsgs)
            {
                foreach (var clientMsgHandler in Messages.ClientHandlers)
                {
                    var handled = clientMsgHandler(ev);
                    if (handled)
                    {
                        return true;
                    }
                }
            }
            if (checkServerMsgs)
            {
                foreach (var serverMsgHandler in Messages.ServerHandlers)
                {
                    var handled = serverMsgHandler(ev);
                    if (handled)
                    {
                        return true;
                    }
                }
            }

            //connection.LogData(source, dest, message);
            return result;
        }
    }
}