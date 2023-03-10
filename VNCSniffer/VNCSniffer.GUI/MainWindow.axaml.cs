using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;

namespace VNCSniffer.GUI
{
    public partial class MainWindow : Window
    {
        public WriteableBitmap WriteableBitmap = new(new PixelSize(300, 300), new Vector(96, 96), Avalonia.Platform.PixelFormat.Rgba8888, Avalonia.Platform.AlphaFormat.Opaque);
        public MainWindow()
        {
            InitializeComponent();
        }

        public void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            if (Design.IsDesignMode)
                return;

            //TODO: init writeablebitmap
            var image = this.FindControl<Image>("framebuffer");
            image.Source = WriteableBitmap;
            unsafe
            {
                using var buffer = WriteableBitmap.Lock();
                var adr = (byte*)buffer.Address;
                for (var y = 0; y < buffer.Size.Height; y++)
                {
                    var rowOffset = y * buffer.RowBytes;
                    for (var x = 0; x < buffer.Size.Width; x++)
                    {
                        var pixelOffset = rowOffset + x * 4;
                        adr[pixelOffset] = 0x00;     // R
                        adr[pixelOffset + 1] = 0xFF; // G
                        adr[pixelOffset + 2] = 0x00; // B
                        adr[pixelOffset + 3] = 0xFF; // A //TODO: can we ignore the alpha part?
                    }
                }
            }
        }
    }
}
