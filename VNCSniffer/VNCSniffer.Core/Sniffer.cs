using Microsoft.VisualBasic;
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
    public static class Sniffer
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

            var config = new DeviceConfiguration()
            {
                Mode = DeviceModes.Promiscuous,
                ReadTimeout = 1000, //TODO: read timeout ms
                Snaplen = 65539, //TODO: set snaplen even higher? this is the min size for loopback fragmented packets
            };
            device.Open(config);
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
                // manually set the ethernet packet in the chain, because of https://github.com/dotpcap/packetnet/issues/115
                var ip = tcpPacket.ParentPacket;
                if (ip != null)
                {
                    if (ip.ParentPacket == null)
                        ip.ParentPacket = p;
                }
                // Pass the packet to the connection manager
                TCPConnectionManager.ProcessPacket(rawPacket.Timeval, tcpPacket);
            }
        }

        private static unsafe void TcpConnectionManager_OnConnectionFound(TcpConnection c)
        {
            if (!Connections.TryGetValue(c, out var con))
            {
                con = new();
                if (typeof(IInjectionDevice).IsAssignableFrom(Device.GetType()))
                    con.Device = (IInjectionDevice)Device;
                Connections.Add(c, con);
            }

            OnConnectionFound.Invoke(null, new(con));
            c.OnPacketReceived += PacketHandler;
        }

        public static void PacketHandler(PosixTimeval timeval, TcpConnection tcpConnection, TcpFlow flow, TcpPacket tcp)
        {
            if (!Connections.TryGetValue(tcpConnection, out var connection))
            {
                Debug.Fail("No connection found for tcp connection.");
                return;
            }

            // collect metadata
            var seq = tcp.SequenceNumber;
            var ack = tcp.AcknowledgmentNumber;
            var window = tcp.WindowSize;

            var ip = (IPPacket)tcp.ParentPacket;
            var sourceIP = ip.SourceAddress;
            var sourcePort = tcp.SourcePort;
            var destIP = ip.DestinationAddress;
            var destPort = tcp.DestinationPort;

            var ethernet = ip.ParentPacket is EthernetPacket ? (EthernetPacket)ip.ParentPacket : null;
            var sourceMac = ethernet?.SourceHardwareAddress;
            var destMac = ethernet?.DestinationHardwareAddress;

            // build participants
            var source = new Participant(sourceIP, sourcePort, sourceMac);
            var dest = new Participant(destIP, destPort, destMac);
            // update source seq/ack
            Participant? conSource = null;
            if (source.Matches(connection.Client))
                conSource = connection.Client;
            else if (source.Matches(connection.Server))
                conSource = connection.Server;

            conSource?.SetTCPData(seq, seq, ack, window);

            // read data
            if (!tcp.HasPayloadData)
                return;

            var msg = tcp.PayloadData;
            if (msg == null || msg.Length == 0)
                return;

            // update next seq number, if conSource found
            conSource?.SetNextSequenceNumber(seq + (uint)msg.Length);

            // (try to) parse packet
            var parsed = ParseMessage(connection, source, dest, msg);
            if (parsed)
                return;

            // send unknownmsg event
            connection.RaiseUnknownMessageEvent(new(tcp, msg));
        }

        private static bool ParseMessage(Connection connection, Participant source, Participant dest, byte[] data)
        {
            var result = false;

            var buffer = data;
            // Check if we have any pakets buffered and if we do append ours
            var conBuffer = connection.GetBuffer(source.IP, source.Port);
            if (conBuffer != null)
            {
                var newBuffer = new byte[conBuffer.Length + buffer.Length];
                conBuffer.CopyTo(newBuffer, 0);
                data.CopyTo(newBuffer, conBuffer.Length);
                buffer = newBuffer;
            }
            // Our connection isnt initialized yet (or so we think)
            // Therefore we check from the laststate if any messages can be parsed
            var ev = new Messages.Messages.MessageEvent(source, dest, connection, buffer);
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
            if (source.Matches(connection.Client))
            {
                // source is the client => only check client msgs
                checkServerMsgs = false;
            }
            else if (source.Equals(connection.Server))
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
                        connection.SetBuffer(source.IP, source.Port, null);
                        return true;
                    }
                    else if (handled == Messages.Messages.ProcessStatus.NeedsMoreBytes)
                    {
                        // save buffer for later processing
                        //TODO: save msg so we can start directly from there, also pls make it directly flow based
                        //Console.WriteLine("Need more bytes");
                        connection.SetBuffer(source.IP, source.Port, buffer);
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
