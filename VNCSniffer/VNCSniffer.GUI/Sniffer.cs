using PacketDotNet;
using PacketDotNet.Connections;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VNCSniffer.Core;
using VNCSniffer.Core.Messages;

namespace VNCSniffer.GUI
{
    public class Sniffer
    {
        static Dictionary<TcpConnection, Connection> Connections = new();
        private static TcpConnectionManager tcpConnectionManager = new();
        static unsafe byte* Framebuffer;
        static int FramebufferLength;

        public static unsafe void Start(byte* framebuffer, int framebufferLength)
        {
            Framebuffer = framebuffer;
            FramebufferLength = framebufferLength;

            string path = "E:\\D\\Visual Studio\\Uni\\Masterarbeit\\Captures\\PythonServerTightClient.pcapng";

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

        private static unsafe void TcpConnectionManager_OnConnectionFound(TcpConnection c)
        {
            if (!Connections.TryGetValue(c, out _))
                Connections.Add(c, new()
                {
                    Framebuffer = Framebuffer,
                    FramebufferLength = FramebufferLength
                });

            c.OnPacketReceived += PacketHandler;
        }

        private static void PacketHandler(PosixTimeval timeval, TcpConnection tcpConnection, TcpFlow flow, TcpPacket tcp)
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

            var data = BitConverter.ToString(msg);
            if (data.Length > 50)
                data = data.Substring(0, 50) + "...";
            Console.WriteLine($"[{tcp.SequenceNumber}] {source}:{sourcePort}->{dest}:{destPort}: {data}");
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
            var ev = new Messages.MessageEvent(source, sourcePort, dest, destPort, connection, buffer);
            if (connection.LastState < State.Initialized)
            {
                for (var i = connection.LastState + 1; i < State.Initialized; i++)
                {
                    var handled = Messages.Handlers[i].Handle(ev);
                    if (handled == Messages.ProcessStatus.Handled)
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
                    if (handled == Messages.ProcessStatus.Handled)
                    {
                        connection.SetBuffer(source, sourcePort, null);
                        return true;
                    }
                    else if (handled == Messages.ProcessStatus.NeedsMoreBytes)
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
                var handled = checkHandlers(Messages.ClientHandlers);
                if (handled)
                    return true;
            }
            if (checkServerMsgs)
            {
                var handled = checkHandlers(Messages.ServerHandlers);
                if (handled)
                    return true;
            }

            //connection.LogData(source, dest, message);
            return result;
        }
    }
}
