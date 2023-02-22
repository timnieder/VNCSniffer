using PacketDotNet;
using SharpPcap;
using System.Data;
using System.Diagnostics;
using System.Runtime.Intrinsics.Arm;

namespace VNCSniffer.Cli
{
    public class Cli
    {
        public static void Main()
        {
            Debug.AutoFlush = true;
            Debug.WriteLine("Start");

            var devices = CaptureDeviceList.Instance;
            if (devices.Count == 0)
            {
                Console.WriteLine("No interfaces found! Make sure WinPcap/Npcap/libpcap is installed.");
                return;
            }

            //TODO: select via config
            var device = devices.FirstOrDefault(x => x.Description.Contains("loopback"));
            if (device == null)
            {
                Console.WriteLine("Unable to find configured device.");
                return;
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
            var data = BitConverter.ToString(msg);
            if (data.Length > 50)
                data = data.Substring(0, 50) + "...";
            Console.WriteLine($"{ip.SourceAddress}:{tcp.SourcePort}->{ip.DestinationAddress}:{tcp.DestinationPort}: {data}");
        }
    }
}