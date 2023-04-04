using PacketDotNet;
using SharpPcap;
using System.Net;
using System.Net.NetworkInformation;
using VNCSniffer.Core.Messages;
using VNCSniffer.Core.Messages.Initialization;

namespace VNCSniffer.Core
{
    public class Participant
    {
        public IPAddress IP;
        public ushort Port;
        // MAC doesn't have to exist, for example in loopback scenarios
        public PhysicalAddress? MAC;

        public uint LastSequenceNumber;
        public uint NextSequenceNumber;
        public uint LastAckNumber;
        public ushort Window;

        public Participant(IPAddress ip, ushort port)
        {
            IP = ip;
            Port = port;
        }

        public Participant(IPAddress ip, ushort port, PhysicalAddress? mac) : this(ip, port)
        {
            MAC = mac;
        }

        public void SetTCPData(uint lastSeq, uint nextSeq, uint lastAck, ushort window)
        {
            LastSequenceNumber = lastSeq;
            NextSequenceNumber = nextSeq;
            LastAckNumber = lastAck;
            Window = window;
        }

        public void SetNextSequenceNumber(uint nextSeq)
        { 
            NextSequenceNumber = nextSeq;
        }

        public bool Matches(IPAddress ip, ushort port)
        {
            return IP.Equals(ip) && Port.Equals(port);
        }

        //TODO: use equals?
        public bool Matches(Participant? src)
        {
            return src != null && Matches(src.IP, src.Port);
        }
    }

    public class Connection
    {
        public State LastState = State.Unknown;

        public IPAddress? Buffer1Address;
        public ushort? Buffer1Port;
        public byte[]? Buffer1;
        public byte[]? Buffer2;

        public string? ProtocolVersion;
        public Participant? Client;
        public Participant? Server;

        public ushort? Width;
        public ushort? Height;
        public PixelFormat? Format;
        public PixelFormat PixelFormat => Format ?? PixelFormat.Default;

        public byte[]? Challenge;
        public byte[]? ChallengeResponse;

        private unsafe byte* framebuffer;
        private int framebufferLength;
        public PixelFormat FramebufferPixelFormat;
        public unsafe Span<byte> Framebuffer => new(framebuffer, framebufferLength);

        // Events
        public event EventHandler<UnknownMessageEvent>? OnUnknownMessage;
        public void RaiseUnknownMessageEvent(UnknownMessageEvent e) => OnUnknownMessage?.Invoke(this, e);
        public event EventHandler<ServerInitEvent>? OnServerInit;
        public void RaiseServerInitEvent(ServerInitEvent e) => OnServerInit?.Invoke(this, e);
        public event EventHandler<ResizeFramebufferEvent>? OnFramebufferResize;
        public void RaiseResizeFramebufferEvent(ResizeFramebufferEvent e) => OnFramebufferResize?.Invoke(this, e);

        public IInjectionDevice? Device;

        public void LogData(Participant source, Participant dest, string text)
        {
            var sourcePrefix = "";
            var destPrefix = "";
            if (source.Matches(Client))
            {
                sourcePrefix = "C";
                destPrefix = "S";
            }
            else if (source.Matches(Server))
            {
                sourcePrefix = "S";
                destPrefix = "C";
            }
            Console.WriteLine($"[{sourcePrefix}]{source.IP}:{source.Port}->[{destPrefix}]{dest.IP}:{dest.Port}: {text}");
        }

        public void SetClientServer(Participant client, Participant server)
        {
            if (Client != null) // don't overwrite
                return;

            Client = client;
            Server = server;
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

        public unsafe void SetFramebuffer(byte* framebuffer, int length, PixelFormat fbFormat)
        {
            this.framebuffer = framebuffer;
            this.framebufferLength = length;
            this.FramebufferPixelFormat = fbFormat;
        }

        public bool SendMessage(Participant src, Participant dst, byte[] content)
        {
            if (Device == null)
                return false;

            // spoof a ethernet+ip+tcp message
            var tcpPacket = new TcpPacket(src.Port, dst.Port)
            {
                //idea: send packet with next sequencenumber, last acknumber
                Acknowledgment = true,
                Push = true,
                AcknowledgmentNumber = src.LastAckNumber,
                SequenceNumber = src.NextSequenceNumber,
                WindowSize = src.Window
            };
            var ipPacket = new IPv4Packet(src.IP, dst.IP)
            {
                //TODO: ip identification
                FragmentFlags = 0b010 // dont fragment
            };

            // stitch packets together
            tcpPacket.PayloadData = content;
            ipPacket.PayloadPacket = tcpPacket;

            if (src.MAC != null)
            {
                var ethernetPacket = new EthernetPacket(src.MAC, dst.MAC, EthernetType.IPv4)
                {
                    PayloadPacket = ipPacket
                };

                // calculate checksum
                /// ip
                ipPacket.UpdateCalculatedValues();
                ipPacket.UpdateIPChecksum();
                /// tcp
                tcpPacket.UpdateCalculatedValues();
                tcpPacket.UpdateTcpChecksum();
                /// ethernet
                ethernetPacket.UpdateCalculatedValues();
                // send
                Device.SendPacket(ethernetPacket);
            }
            else //TODO: doesnt work on local networks
            {
                //Device.SendPacket(ipPacket);
                return false;
            }
            return true;
        }

        //TODO: merge some code with the SendMessage func
        public bool ResetConnection(Participant src, Participant dst)
        {
            if (Device == null)
                return false;

            // spoof a ip+tcp message
            var tcpPacket = new TcpPacket(src.Port, dst.Port)
            {
                //idea: send packet with rst flag
                AcknowledgmentNumber = src.LastAckNumber,
                SequenceNumber = src.NextSequenceNumber,
                WindowSize = src.Window,
                Reset = true,
            };
            var ipPacket = new IPv4Packet(src.IP, dst.IP)
            {
                //TODO: ip identification
                FragmentFlags = 0b010 // dont fragment
            };
            
            if (src.MAC != null)
            {
                var ethernetPacket = new EthernetPacket(src.MAC, dst.MAC, EthernetType.IPv4)
                {
                    PayloadPacket = ipPacket
                };

                // calculate checksum
                /// ip
                ipPacket.UpdateCalculatedValues();
                ipPacket.UpdateIPChecksum();
                /// tcp
                tcpPacket.UpdateCalculatedValues();
                tcpPacket.UpdateTcpChecksum();
                /// ethernet
                ethernetPacket.UpdateCalculatedValues();
                // send
                Device.SendPacket(ethernetPacket);
            }
            else
            {
                return false;
            }

            return false;
        }

        // Drawing
        /// <summary>
        /// Copies a region inside the buffer to another region inside the buffer.
        /// </summary>
        /// <param name="srcX"></param>
        /// <param name="srcY"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="destX"></param>
        /// <param name="destY"></param>
        public unsafe void CopyRegion(ushort srcX, ushort srcY, ushort w, ushort h, ushort destX, ushort destY)
        {
            if (framebuffer == null)
                return;

            //TODO: try to guess bpp
            var format = PixelFormat;
            var bpp = format.BitsPerPixel;
            bpp /= 8;
            if (Width == null) //TODO: handle this case
                return;
            var bytesPerRow = Width.Value * bpp;
            var srcOffset = srcY * bytesPerRow + srcX * bpp;
            var destOffset = destY * bytesPerRow + destX * bpp;
            var fBuffer = Framebuffer;
            //TODO: framebuffer size checks, resize if too small
            var dataLength = (w * h * bpp);
            if (fBuffer.Length < (srcOffset + dataLength))
                return;
            if (fBuffer.Length < (destOffset + dataLength))
                return;

            if (w == Width)
            {
                fBuffer[srcOffset..(srcOffset + dataLength)].CopyTo(fBuffer[destOffset..]);
            }
            else // if we've got a smaller rect, we need to copy per line
            {
                var lineLength = w * bpp;
                for (var i = 0; i < h; i++)
                {
                    var lineOffset = i * bytesPerRow;
                    fBuffer[(srcOffset + lineOffset)..(srcOffset + lineOffset + lineLength)].CopyTo(fBuffer[(destOffset + lineOffset)..]);
                }
            }
        }

        /// <summary>
        /// Copies the region from the given buffer to the framebuffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        public unsafe void DrawRegion(ReadOnlySpan<byte> buffer, ushort x, ushort y, ushort w, ushort h, byte? forceBpp = null)
        {
            if (framebuffer == null)
                return;

            var format = PixelFormat;
            var bpp = format.BitsPerPixel;
            bpp /= 8;
            if (forceBpp != null)
                bpp = forceBpp.Value;
            if (Width == null) //TODO: handle this case
                return;

            var targetBpp = FramebufferPixelFormat.BitsPerPixel / 8;
            var bytesPerRow = Width.Value * targetBpp;
            var offset = y * bytesPerRow + x * targetBpp;
            var fBuffer = Framebuffer;
            //TODO: framebuffer size checks, resize if too small
            // TODO: check if big endian
            if (bpp != targetBpp) // if we've got a different bpp in the buffer we need to copy per pixel
            {
                //TODO: make this code more sane
                for (var i = 0; i < h; i++)
                {
                    var lineOffset = offset + i * bytesPerRow;
                    var bLineOffset = i * w * bpp;
                    for (var j = 0; j < w; j++)
                    {
                        var off = lineOffset + j * targetBpp;
                        var bOff = bLineOffset + j * bpp;
                        // Check if in buffer has enough bytes
                        if (buffer.Length < (bOff + bpp))
                            return;

                        // Check if framebuffer has enough space
                        if (fBuffer.Length < (off + targetBpp))
                            return;

                        var clr = new Color(buffer[bOff..], format, bpp, FramebufferPixelFormat);
                        //TODO: adjust according to fbpixelformat
                        fBuffer[off] = clr.B; // b
                        fBuffer[off + 1] = clr.G; // g
                        fBuffer[off + 2] = clr.R; // r
                        fBuffer[off + 3] = 0xFF; // alpha
                    }
                }
                return;
            }
            if (w == Width)
            {
                // Check if we write too much
                if (buffer.Length > (fBuffer.Length - offset))
                    return;

                buffer.CopyTo(fBuffer[offset..]);
                // overwrite alpha //TODO: adjust according to fbpixelformat
                for (var i = (targetBpp - 1); i < buffer.Length; i += targetBpp)
                {
                    fBuffer[offset + i] = 0xFF;
                }
            }
            else // if we've got a smaller rect, we need to copy per line
            {
                var lineLength = w * bpp;
                for (var i = 0; i < h; i++)
                {
                    var fbLineOffset = i * bytesPerRow;
                    var inLineOffset = i * lineLength;
                    // Check if in buffer has enough bytes
                    if (buffer.Length < inLineOffset + lineLength)
                        return;

                    // Check if framebuffer has enough space
                    if (fBuffer.Length < (offset + fbLineOffset + lineLength))
                        return;

                    buffer[inLineOffset..(inLineOffset + lineLength)].CopyTo(fBuffer[(offset + fbLineOffset)..]);
                    // overwrite alpha //TODO: adjust according to fbpixelformat
                    for (var j = (targetBpp - 1); j < lineLength; j += targetBpp)
                    {
                        fBuffer[offset + fbLineOffset + j] = 0xFF;
                    }
                }
            }

            //TODO: merge alpha overwrite?
        }

        /// <summary>
        /// Draws one or multiple pixels in a line. Doesn't support line wrapping.
        /// </summary>
        /// <param name="clr"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="length"></param>
        public unsafe void DrawPixel(Color clr, ushort x, ushort y, ushort length = 1)
        {
            if (framebuffer == null)
                return;

            var format = FramebufferPixelFormat;
            var bpp = format.BitsPerPixel;
            bpp /= 8;
            if (Width == null) //TODO: handle this case
                return;
            var bytesPerRow = Width.Value * bpp;
            var offset = y * bytesPerRow + x * bpp;
            var fBuffer = Framebuffer;

            for (var i = 0; i < length; i++)
            {
                var off = offset + i * bpp;
                // Check if framebuffer has enough space
                if (fBuffer.Length < (off + bpp))
                    return;

                //TODO: adjust according to fbpixelformat
                fBuffer[off] = clr.B; // b
                fBuffer[off + 1] = clr.G; // g
                fBuffer[off + 2] = clr.R; // r
                fBuffer[off + 3] = 0xFF; // alpha
            }
        }

        /// <summary>
        /// Draws a rectangle in one color.
        /// </summary>
        /// <param name="clr"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        public unsafe void DrawSolidRect(Color clr, ushort x, ushort y, ushort w, ushort h)
        {
            if (framebuffer == null)
                return;

            var format = FramebufferPixelFormat;
            var bpp = format.BitsPerPixel;
            bpp /= 8;
            if (Width == null) //TODO: handle this case
                return;
            var bytesPerRow = Width.Value * bpp;
            var offset = y * bytesPerRow + x * bpp;
            var fBuffer = Framebuffer;
            //TODO: framebuffer size checks, resize if too small
            //TODO: check if big endian
            // insert clr line by line //TODO: can we optimize this?
            for (var i = 0; i < h; i++)
            {
                var lineOffset = offset + i * bytesPerRow;
                for (var j = 0; j < w; j++)
                {
                    var off = lineOffset + j * bpp;
                    // Check if framebuffer has enough space
                    if (fBuffer.Length < (off + bpp))
                        return;

                    //TODO: adjust according to fbpixelformat
                    fBuffer[off] = clr.B; // b
                    fBuffer[off + 1] = clr.G; // g
                    fBuffer[off + 2] = clr.R; // r
                    fBuffer[off + 3] = 0xFF; // alpha
                }
            }
        }
    }

    public class UnknownMessageEvent : EventArgs
    {
        public byte[] Data;
        public TcpPacket TCP; //TODO: only copy what we need like source/dest
        public UnknownMessageEvent(TcpPacket tcp, byte[] data)
        {
            TCP = tcp;
            Data = data;
        }
    }

    public class ResizeFramebufferEvent : EventArgs
    {
        public ushort Width;
        public ushort Height; //TODO: is this enough? smth like keep buffer? pixelformat?
        public ResizeFramebufferEvent(ushort width, ushort height)
        {
            Width = width;
            Height = height;
        }
    }
}
