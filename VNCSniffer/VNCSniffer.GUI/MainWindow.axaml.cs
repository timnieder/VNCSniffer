using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using System.Runtime.CompilerServices;

namespace VNCSniffer.GUI
{
    public partial class MainWindow : Window
    {
        public WriteableBitmap Bitmap = new(new PixelSize(300, 300), new Vector(96, 96), Avalonia.Platform.PixelFormat.Rgba8888, Avalonia.Platform.AlphaFormat.Opaque);
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

            //TODO: init writeablebitmap
            var image = this.FindControl<Image>("framebuffer");
            image.Source = Bitmap;

            using var bmp = Bitmap.Lock(); //TODO: need using?
            //TODO: instead do this event based?
            //TODO: this currently blocks the UI thread
            unsafe
            {
                Sniffer.Start((byte*)bmp.Address, bmp.RowBytes * bmp.Size.Height);
            }
            /*
            unsafe
            {
                var adr = (byte*)bmp.Address;
                for (var y = 0; y < bmp.Size.Height; y++)
                {
                    var rowOffset = y * bmp.RowBytes;
                    for (var x = 0; x < bmp.Size.Width; x++)
                    {
                        var pixelOffset = rowOffset + x * 4;
                        adr[pixelOffset] = 0x00;     // R
                        adr[pixelOffset + 1] = 0xFF; // G
                        adr[pixelOffset + 2] = 0x00; // B
                        adr[pixelOffset + 3] = 0xFF; // A //TODO: can we ignore the alpha part?
                    }
                }
            }*/
        }
    }
}
