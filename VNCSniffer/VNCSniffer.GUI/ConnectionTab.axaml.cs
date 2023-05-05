using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VNCSniffer.Core;
using VNCSniffer.GUI.ViewModels;

namespace VNCSniffer.GUI
{
    public partial class ConnectionTab : UserControl
    {
        public ConnectionTab(ConnectionTabViewModel vm) : this()
        {
            DataContext = vm;
            vm.Image = Framebuffer; //TODO: this is probably not how you mvvm
        }

        public ConnectionTab()
        {
            InitializeComponent();
        }
    }
}
