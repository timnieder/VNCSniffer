using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNCSniffer.Core;

namespace VNCSniffer.GUI.ViewModels
{
    public class ConnectionTabViewModel : ReactiveObject
    {
        private string headerName;
        public string HeaderName
        {
            get => headerName;
            set => this.RaiseAndSetIfChanged(ref headerName, value);
        }
        private WriteableBitmap bitmap;
        public WriteableBitmap Bitmap
        {
            get => bitmap;
            set => this.RaiseAndSetIfChanged(ref bitmap, value);
        }


        public unsafe void ResizeFramebuffer(Connection con, int width, int height)
        {
            //TODO: rather resize than destroy it?
            Bitmap = new(new(width, height), new Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Opaque);
            using var bmp = Bitmap.Lock(); //TODO: need using?
            unsafe
            {
                var address = (byte*)bmp.Address;
                var length = bmp.RowBytes * bmp.Size.Height;
                con.SetFramebuffer(address, length);
            }

            /*
            // need to set some things from the ui thread (like the image)
            Dispatcher.UIThread.Post(() =>
            {
                Framebuffer.Source = Bitmap;
            });*/
        }
    }
}
