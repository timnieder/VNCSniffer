using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace VNCSniffer.Cli
{
    public class Cli
    {
        static Connection connection = new();
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

            //TODO: create better filter? also let user select port?
            device.Open(DeviceModes.Promiscuous); //TODO: read timeout ms
            var filter = "tcp and " +
                    "(port 5900 or " +
                    "port 5901)";
            device.Filter = filter;
            device.OnPacketArrival += PacketHandler;
            Console.WriteLine("Listening...");
            device.Capture();
            device.Close();
        }

        private static void PacketHandler(object s, PacketCapture packet)
        {
            var raw = packet.GetPacket();
            var p = Packet.ParsePacket(raw.LinkLayerType, raw.Data);
            var ip = p.Extract<IPPacket>();
            var tcp = p.Extract<TcpPacket>();

            if (!tcp.HasPayloadData)
                return;

            if (!tcp.Push) //TODO: check if they only push
                return;

            //TODO: handle multi packet msgs?
            var msg = tcp.PayloadData;
            if (msg == null || msg.Length == 0)
                return;

            var source = ip.SourceAddress;
            var dest = ip.DestinationAddress;

            var parsed = ParseMessage(source, dest, msg);
            if (parsed)
                return;

            var data = BitConverter.ToString(msg);
            if (data.Length > 50)
                data = data.Substring(0, 50) + "...";
            Console.WriteLine($"{source}:{tcp.SourcePort}->{dest}:{tcp.DestinationPort}: {data}");
        }

        private static bool ParseMessage(IPAddress source, IPAddress dest, byte[] data)
        {
            var message = "";
            var result = false;
            // Our connection isnt initialized yet (or so we think)
            // Therefore we check from the laststate if any messages can be parsed
            var ev = new Messages.MessageEvent(source, dest, connection, data);
            if (connection.LastState < State.Initialized - 1)
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
            }
            else
            {
                var checkClientMsgs = true;
                var checkServerMsgs = true;
                // TODO: parse c2s/s2c messages
                if (source.Equals(connection.Client))
                {
                    // source is the client => only check client msgs
                    checkServerMsgs = false;
                }
                else if (source.Equals(connection.Server))
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
            }
            //connection.LogData(source, dest, message);
            return result;
        }
    }
}