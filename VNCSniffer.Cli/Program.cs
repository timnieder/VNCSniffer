using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Diagnostics;
using VNCSniffer.Core;
using System;

namespace VNCSniffer.Cli
{
    public class Cli
    {
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
            Sniffer.Start(device, new int[] { 5900, 5901 });
            Sniffer.OnConnectionFound += Sniffer_OnConnectionFound;
            Console.WriteLine("Press Enter to stop...");
            Console.ReadLine();
        }

        private static void Sniffer_OnConnectionFound(object? sender, ConnectionFoundEvent e)
        {
            Console.WriteLine($"New connection found");
            e.Connection.OnUnknownMessage += Connection_OnUnknownMessage;
        }

        private static void Connection_OnUnknownMessage(object? sender, UnknownMessageEvent e)
        {
            if (sender is not Connection)
                return;

            var data = BitConverter.ToString(e.Data);
            if (data.Length > 50)
                data = $"{data.AsSpan(0, 50)}...";

            var ip = (IPPacket)e.TCP.ParentPacket;
            var source = ip.SourceAddress;
            var sourcePort = e.TCP.SourcePort;
            var dest = ip.DestinationAddress;
            var destPort = e.TCP.DestinationPort;
            Console.WriteLine($"[{e.TCP.SequenceNumber}] {source}:{sourcePort}->{dest}:{destPort}: {data}");
        }
    }
}