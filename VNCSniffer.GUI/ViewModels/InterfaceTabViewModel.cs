using Avalonia.Controls;
using ReactiveUI;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Collections.ObjectModel;
using System.IO;

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
            MainWindow.OnDeviceSelected(iface.Device);
        }

        public async void OnLoadFromFileButtonClick()
        {
            var dialog = new OpenFileDialog()
            {
                AllowMultiple = false,
                Filters = new()
                {
                    new()
                    {
                        Name = "PCAP Files",
                        Extensions = new(){ "pcap", "pcapng" }
                    },
                    new()
                    {
                        Name = "All Files",
                        Extensions = new(){ "*" }
                    }
                },
            };
            var files = await dialog.ShowAsync(MainWindow);
            if (files == null || files.Length == 0) // Nothing given
                return;

            var filePath = files[0];
            if (!File.Exists(filePath))
                return;

            MainWindow.OnDeviceSelected(new CaptureFileReaderDevice(filePath));
        }
    }
}
