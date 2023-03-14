using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
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
        public static unsafe void Start()
        {
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

            //TODO: create better filter? also let user select port?
            Core.Sniffer.Start(device, new int[] { 5900, 5901 });
            Core.Sniffer.OnConnectionFound += Sniffer_OnConnectionFound;
        }

        private static void Sniffer_OnConnectionFound(object sender, ConnectionFoundEvent e)
        {
            //TODO: create new tab
            e.Connection.OnServerInit += Connection_OnServerInit;
        }

        private static unsafe void Connection_OnServerInit(object? sender, Core.Messages.Initialization.ServerInitEvent e)
        {
            if (sender is not Connection)
                return;

            // notify the bitmap holder to resize the framebuffer
            var con = (Connection)sender;
            //TODO: rather cache mainwindow on startup?
            var lifetime = (IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime;
            var mainWindow = (MainWindow)lifetime.MainWindow;
            mainWindow.ResizeFramebuffer(con, e.Width, e.Height);
        }
    }
}
