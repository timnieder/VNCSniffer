using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Linq;
using VNCSniffer.Core;

namespace VNCSniffer.GUI
{
    public class Sniffer
    {
        public static MainWindow MainWindow;
        public static unsafe void Start(MainWindow window)
        {
            string path = null;
            //path = "C:\\Users\\Exp\\Desktop\\1.pcapng";
            //path = "E:\\D\\Visual Studio\\Uni\\Masterarbeit\\Captures\\Encodings\\Raw.pcapng";
            //path = "E:\\D\\Visual Studio\\Uni\\Masterarbeit\\Captures\\Encodings\\RRE.pcapng";
            //path = "E:\\D\\Visual Studio\\Uni\\Masterarbeit\\Captures\\Encodings\\Hextile.pcapng";
            //path = "E:\\D\\Visual Studio\\Uni\\Masterarbeit\\Captures\\Encodings\\TRLE.pcapng";
            //path = "E:\\D\\Visual Studio\\Uni\\Masterarbeit\\Captures\\Encodings\\ZRLE.pcapng";

            MainWindow = window;

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
            Core.Sniffer.Start(device, new int[] { 5900, 5901 });
            Core.Sniffer.OnConnectionFound += Sniffer_OnConnectionFound;
        }

        private static void Sniffer_OnConnectionFound(object sender, ConnectionFoundEvent e)
        {
            //TODO: create new tab
            e.Connection.OnServerInit += Connection_OnServerInit;
            e.Connection.OnFramebufferResize += Connection_OnFramebufferResize;
        }

        private static void Connection_OnFramebufferResize(object? sender, ResizeFramebufferEvent e)
        {
            if (sender is not Connection)
                return;

            // notify the bitmap holder to resize the framebuffer
            var con = (Connection)sender;
            MainWindow.ResizeFramebuffer(con, e.Width, e.Height);
        }

        private static unsafe void Connection_OnServerInit(object? sender, Core.Messages.Initialization.ServerInitEvent e)
        {
            if (sender is not Connection)
                return;

            // notify the bitmap holder to resize the framebuffer
            var con = (Connection)sender;
            MainWindow.ResizeFramebuffer(con, e.Width, e.Height);
        }
    }
}
