using Avalonia.Controls;
using Avalonia.Interactivity;
using VNCSniffer.GUI.ViewModels;

namespace VNCSniffer.GUI
{
    public partial class InterfaceTab : UserControl
    {
        public InterfaceTab(InterfaceTabViewModel vm) : this()
        {
            DataContext = vm;
        }

        public InterfaceTab()
        {
            InitializeComponent();
        }
    }
}
