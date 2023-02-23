using Microsoft.VisualBasic;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Numerics;
using System.Runtime.Intrinsics.Arm;
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
            if (connection.State == State.Unknown)
            {
                var str = Encoding.Default.GetString(data);
                if (str.StartsWith("RFB")) // RFB
                {
                    if (connection.ProtocolVersion == null) // version not yet set, state unknown
                    {
                        connection.ProtocolVersion = str;
                    }
                    else // version set, therefore we got a message before, this is the client
                    {
                        connection.SetClientServer(source, dest); // sent by client
                    }

                    connection.LogData(source, dest, $"ProtocolVersion: {str.TrimEnd()}");
                    return true;
                }

                // Security Types
                var numberOfSecurityTypes = data[0];
                if (data.Length == (1 + numberOfSecurityTypes))
                {
                    var encodings = string.Join(" ", data.Skip(1));
                    connection.SetClientServer(dest, source); // sent by server
                    connection.LogData(source, dest, $"Security Types ({numberOfSecurityTypes}): {encodings}");
                    return true;
                }

                // VNC Auth
                if (data.Length == 16)
                {
                    if (connection.Challenge != null) // already got a challenge, this is the response 
                    {
                        connection.ChallengeResponse = data;
                        connection.SetClientServer(source, dest); // sent by client
                        connection.LogData(source, dest, $"Response: {BitConverter.ToString(data)}");
                    }
                    else // no challenge cached
                    {
                        var unsure = "?";
                        if (connection.Client == null)
                            unsure = "";
                        connection.Challenge = data;
                        connection.LogData(source, dest, $"Challenge{unsure}: {BitConverter.ToString(data)}");
                    }
                    return true;
                }
                // Auth result
                if (data.Length == 4)
                {
                    // sent by server
                    connection.SetClientServer(dest, source);
                }

                //TODO: tryparse c2s/s2c to see if we're initialized already
            }
            else if (connection.State == State.Initialized)
            {
                //TODO: parse c2s/s2c messages
            }
            return false;
        }
    }
}