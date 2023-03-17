using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using ReactiveUI;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VNCSniffer.GUI.ViewModels
{
    public class Interface
    {
        public string Text;
        public ILiveDevice Device;
        public Interface(string text, ILiveDevice device)
        {
            Text = text;
            Device = device;
        }

        public override string ToString()
        {
            return Text;
        }
    }

    public class InterfaceTabViewModel : ReactiveObject
    {
        private MainWindow MainWindow;
        private ObservableCollection<Interface> interfaces = new();
        public ObservableCollection<Interface> Interfaces
        {
            get => interfaces;
            set => this.RaiseAndSetIfChanged(ref interfaces, value);
        }

        public InterfaceTabViewModel(MainWindow mainWindow)
        {
            MainWindow = mainWindow;
            if (Design.IsDesignMode)
                return;

            Refresh();
        }

        public void Refresh()
        {
            // refresh interfaces
            CaptureDeviceList.Instance.Refresh();
            // add them to the list
            Interfaces.Clear();
            foreach (var iface in CaptureDeviceList.Instance)
            {
                Interfaces.Add(new($"{iface.Description} ({iface.Name})", iface));
            }
        }

        public void OnRefreshButtonClick() => Refresh();

        public void OnInterfaceSelected(Interface iface)
        {
            MainWindow.OnInterfaceSelected(iface.Device);
        }
    }
}
