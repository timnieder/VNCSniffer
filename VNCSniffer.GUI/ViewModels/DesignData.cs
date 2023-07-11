using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VNCSniffer.GUI.ViewModels
{
    public static class DesignData
    {
        public static InterfaceTabViewModel InterfaceTab = new(null)
        {
            Interfaces = new() { new("Loopback", null), new("Ethernet 1", null) }
        };
    }
}
