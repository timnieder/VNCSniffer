using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

        public void OnImageClicked(object sender, RoutedEventArgs ev)
        {
            ((ConnectionTabViewModel)DataContext!).OnImageClicked((TappedEventArgs)ev);
        }
    }
}
