using PacketDotNet;
using PacketDotNet.Connections;
using SharpPcap;
using System.Diagnostics;
using System.Net;
using VNCSniffer.Core.Messages;

namespace VNCSniffer.Core
{
    public class ConnectionFoundEvent : EventArgs
    {
        public Connection Connection; //TODO: add the ipaddresses
        public ConnectionFoundEvent(Connection connection)
        {
            Connection = connection;
        }
    }
    public class Sniffer
    {
        public static Dictionary<TcpConnection, Connection> Connections = new();
        public static TcpConnectionManager TCPConnectionManager = new();
        public static ICaptureDevice Device;
        //public static CaptureFileWriterDevice WriterDevice;

        public static event EventHandler<ConnectionFoundEvent> OnConnectionFound;

        public static void Start(ICaptureDevice device, int[] ports)
        {
            Device = device;

            if (ports.Length == 0)
                return;

            TCPConnectionManager.OnConnectionFound += TcpConnectionManager_OnConnectionFound;

            device.Open(DeviceModes.Promiscuous); //TODO: read timeout ms
            var filter = $"tcp and (port {ports[0]}";
            foreach (var port in ports)
            {
                filter += $" or port {port}";
            }
            filter += ")";
            device.Filter = filter;
            device.OnPacketArrival += Device_OnPacketArrival;

            //WriterDevice = new("1.pcap", FileMode.Create);
            //WriterDevice.Open(device);
            device.StartCapture();
        }

        public static void Stop()
        {
            Device?.StopCapture();
        }

        private static void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            var rawPacket = e.GetPacket();
            //WriterDevice.Write(rawPacket);
            var p = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

            var tcpPacket = p.Extract<TcpPacket>();

            if (tcpPacket != null)
            {
                // Pass the packet to the connection manager
                TCPConnectionManager.ProcessPacket(rawPacket.Timeval, tcpPacket);
            }
        }

        private static unsafe void TcpConnectionManager_OnConnectionFound(TcpConnection c)
        {
            if (!Connections.TryGetValue(c, out var con))
            {
                con = new();
                Connections.Add(c, con);
            }

            OnConnectionFound.Invoke(null, new(con));
            c.OnPacketReceived += PacketHandler;
        }

        public static void PacketHandler(PosixTimeval timeval, TcpConnection tcpConnection, TcpFlow flow, TcpPacket tcp)
        {
            if (!tcp.HasPayloadData)
                return;

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
                Debug.Fail("No connection found for tcp connection.");
                return;
            }

            var parsed = ParseMessage(connection, source, sourcePort, dest, destPort, msg);
            if (parsed)
                return;

            // send unknownmsg event
            connection.RaiseUnknownMessageEvent(new(tcp, msg));
        }

        private static bool ParseMessage(Connection connection, IPAddress source, ushort sourcePort, IPAddress dest, ushort destPort, byte[] data)
        {
            var result = false;

            var buffer = data;
            // Check if we have any pakets buffered and if we do append ours
            var conBuffer = connection.GetBuffer(source, sourcePort);
            if (conBuffer != null)
            {
                var newBuffer = new byte[conBuffer.Length + buffer.Length];
                conBuffer.CopyTo(newBuffer, 0);
                data.CopyTo(newBuffer, conBuffer.Length);
                buffer = newBuffer;
            }
            // Our connection isnt initialized yet (or so we think)
            // Therefore we check from the laststate if any messages can be parsed
            var ev = new Messages.Messages.MessageEvent(source, sourcePort, dest, destPort, connection, buffer);
            if (connection.LastState < State.Initialized)
            {
                for (var i = connection.LastState + 1; i < State.Initialized; i++)
                {
                    var handled = Messages.Messages.Handlers[i].Handle(ev);
                    if (handled == Messages.Messages.ProcessStatus.Handled)
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

            bool checkHandlers(List<IVNCMessage> handlers)
            {
                foreach (var msgHandler in handlers)
                {
                    var handled = msgHandler.Handle(ev);
                    if (handled == Messages.Messages.ProcessStatus.Handled)
                    {
                        connection.SetBuffer(source, sourcePort, null);
                        return true;
                    }
                    else if (handled == Messages.Messages.ProcessStatus.NeedsMoreBytes)
                    {
                        // save buffer for later processing
                        //TODO: save msg so we can start directly from there, also pls make it directly flow based
                        //Console.WriteLine("Need more bytes");
                        connection.SetBuffer(source, sourcePort, buffer);
                        return true;
                    }
                }
                return false;
            }
            if (checkClientMsgs)
            {
                var handled = checkHandlers(Messages.Messages.ClientHandlers);
                if (handled)
                    return true;
            }
            if (checkServerMsgs)
            {
                var handled = checkHandlers(Messages.Messages.ServerHandlers);
                if (handled)
                    return true;
            }

            return result;
        }
    }
}
