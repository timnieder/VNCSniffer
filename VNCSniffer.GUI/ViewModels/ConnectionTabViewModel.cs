using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using ReactiveUI;
using System;
using System.Diagnostics;
using VNCSniffer.Core;
using VNCSniffer.Core.Messages.Client;

namespace VNCSniffer.GUI.ViewModels
{
    public class ConnectionTabViewModel : ReactiveObject
    {
        private Connection Connection;
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

        public Image? Image = null;

        public static readonly PixelFormat FBPixelFormat = new PixelFormat()
        {
            BitsPerPixel = 32,
            Depth = 24,
            BigEndian = false,
            TrueColor = true,
            RedMax = 255,
            GreenMax = 255,
            BlueMax = 255,
            RedShift = 16,
            GreenShift = 8,
            BlueShift = 0,
        };

        public ConnectionTabViewModel(Connection connection)
        {
            this.Connection = connection;
        }

        public unsafe void ResizeFramebuffer(Connection con, int width, int height)
        {
            //TODO: rather resize than destroy it?
            //TODO: needs lock?
            try
            {
                Bitmap = new(new(width, height), new Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Opaque);
            } 
            catch (Exception e) 
            {
                Debug.Fail($"Exception during Bitmap resizing: {e}");
                return;
            }
            using var bmp = Bitmap.Lock(); //TODO: need using?
            unsafe
            {
                var address = (byte*)bmp.Address;
                var length = bmp.RowBytes * bmp.Size.Height;
                con.SetFramebuffer(address, length, FBPixelFormat);
            }

            // bitmap notifies image automatically that it has changed //TODO: apparently not. fix thisss
        }

        public void RefreshFramebuffer(Connection con) 
        {
            if (Image == null)
                throw new Exception("Image not set");

            Image.InvalidateVisual();
        }

        public void OnRefreshButtonClick(Image img)
        {
            // Repaint framebuffer
            img.InvalidateVisual();
        }

        public void OnImageClicked(TappedEventArgs ev)
        {
            var pos = ev.GetPosition(Image);
            Console.WriteLine(pos);
            var relX = pos.X / Image!.Bounds.Width;
            var relY = pos.Y / Image!.Bounds.Height;
            var x = (ushort)(relX * Bitmap.Size.Width);
            var y = (ushort)(relY * Bitmap.Size.Height);
            SendPointerMove(x, y);
        }

        public void OnSendButtonClick()
        {
            SendPointerMove(100, 100);
        }

        public void OnResetButtonClick()
        {
            Connection.ResetConnection(Connection.Server!, Connection.Client!);
            Connection.ResetConnection(Connection.Client!, Connection.Server!);
        }

        public void SendPointerMove(ushort x, ushort y)
        {
            var msg = PointerEvent.Build(0, x, y);
            Connection.SendMessage(Connection.Client!, Connection.Server!, msg);
        }
    }
}
