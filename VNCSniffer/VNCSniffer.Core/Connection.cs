﻿using PacketDotNet;
using System.Net;
using VNCSniffer.Core.Messages;
using VNCSniffer.Core.Messages.Initialization;

namespace VNCSniffer.Core
{
    public class Connection
    {
        public State LastState = State.Unknown;

        public IPAddress? Buffer1Address;
        public ushort? Buffer1Port;
        public byte[]? Buffer1;
        public byte[]? Buffer2;

        public string? ProtocolVersion;
        //TODO: change those into a class containg both ip and port (or use flows?)
        public IPAddress? Client;
        public ushort? ClientPort;
        public IPAddress? Server;
        public ushort? ServerPort;

        public ushort? Width;
        public ushort? Height;
        public PixelFormat? Format;

        public byte[]? Challenge;
        public byte[]? ChallengeResponse;

        private unsafe byte* framebuffer;
        private int framebufferLength;
        public unsafe Span<byte> Framebuffer => new(framebuffer, framebufferLength);

        // Events
        public event EventHandler<UnknownMessageEvent> OnUnknownMessage;
        public void RaiseUnknownMessageEvent(UnknownMessageEvent e) => OnUnknownMessage?.Invoke(this, e);
        public event EventHandler<ServerInitEvent> OnServerInit;
        public void RaiseServerInitEvent(ServerInitEvent e) => OnServerInit?.Invoke(this, e);


        public void LogData(IPAddress source, ushort sourcePort, IPAddress dest, ushort destPort, string text)
        {
            var sourcePrefix = "";
            var destPrefix = "";
            if (source.Equals(Client) && sourcePort.Equals(ClientPort))
            {
                sourcePrefix = "C";
                destPrefix = "S";
            }
            else if (source.Equals(Server) && destPort.Equals(ServerPort))
            {
                sourcePrefix = "S";
                destPrefix = "C";
            }
            Console.WriteLine($"[{sourcePrefix}]{source}:{sourcePort}->[{destPrefix}]{dest}:{destPort}: {text}");
        }

        public void SetClientServer(IPAddress client, ushort clientPort, IPAddress server, ushort serverPort)
        {
            if (Client != null) // don't overwrite
                return;

            Client = client;
            ClientPort = clientPort;
            Server = server;
            ServerPort = serverPort;
        }

        public byte[]? GetBuffer(IPAddress address, ushort port)
        {
            // Buffer not assigned yet
            if (Buffer1Address == null)
            {
                Buffer1Address = address;
                Buffer1Port = port;
                return Buffer1;
            }

            if (address.Equals(Buffer1Address) && port.Equals(Buffer1Port))
                return Buffer1;
            else
                return Buffer2;
        }

        public void SetBuffer(IPAddress address, ushort port, byte[]? buffer)
        {
            // Buffer not assigned yet
            if (Buffer1Address == null)
            {
                Buffer1Address = address;
                Buffer1Port = port;
                Buffer1 = buffer;
            }

            if (address.Equals(Buffer1Address) && port.Equals(Buffer1Port))
                Buffer1 = buffer;
            else
                Buffer2 = buffer;
        }

        public unsafe void SetFramebuffer(byte* framebuffer, int length)
        {
            this.framebuffer = framebuffer;
            this.framebufferLength = length;
        }

        // Drawing
        public unsafe void DrawRegion(ReadOnlySpan<byte> buffer, ushort x, ushort y)
        {
            if (framebuffer == null)
                return;

            var bpp = Format != null ? Format.BitsPerPixel : 32;
            bpp /= 8;
            if (Width == null) //TODO: handle this case
                return;
            var bytesPerRow = Width.Value * bpp;
            var offset = y * bytesPerRow + x * bpp;
            var fBuffer = Framebuffer;
            //TODO: framebuffer size checks, resize if too small
            buffer.CopyTo(fBuffer);
            // TODO: check if big endian

            // overwrite alpha
            for (var i = 3; i < buffer.Length; i += 4)
            {
                fBuffer[offset + i] = 0xFF;
            }
        }

        public void DrawPixel(byte[] clr, ushort x, ushort y)
        {
            //TODO: drawpixel/setpixel
        }

        public void DrawSolidRect(byte[] clr, ushort x, ushort y, ushort w, ushort h)
        {
            //TODO: drawsolidrect
        }
    }

    public class UnknownMessageEvent : EventArgs
    {
        public byte[] Data;
        public TcpPacket TCP; //TODO: only copy what we need like source/dest
        public UnknownMessageEvent(TcpPacket tcp, byte[] data)
        {
            tcp = tcp;
            Data = data;
        }
    }
}
