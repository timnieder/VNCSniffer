using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System.Runtime.CompilerServices;
using VNCSniffer.Core;

namespace VNCSniffer.GUI
{
    public partial class MainWindow : Window
    {
        public WriteableBitmap Bitmap;
        private Image Image;
        public MainWindow()
        {
            InitializeComponent();
        }

        public void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            if (Design.IsDesignMode)
                return;

#if DEBUG
            this.AttachDevTools();
#endif
            Image = this.FindControl<Image>("framebuffer");

            Sniffer.Start(this);
        }

        public unsafe void ResizeFramebuffer(Connection con, int width, int height)
        {
            //TODO: rather resize than destroy it?
            Bitmap = new(new(width, height), new Vector(96, 96), Avalonia.Platform.PixelFormat.Rgba8888, Avalonia.Platform.AlphaFormat.Opaque);
            using var bmp = Bitmap.Lock(); //TODO: need using?
            unsafe
            {
                var address = (byte*)bmp.Address;
                var length = bmp.RowBytes * bmp.Size.Height;
                con.SetFramebuffer(address, length);
            }

            // need to set some things from the ui thread (like the image)
            Dispatcher.UIThread.Post(() =>
            {
                Image.Source = Bitmap;
            });
        }
    }
}
