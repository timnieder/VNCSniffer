using PacketDotNet.Tcp;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Drawing;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Messages.Server
{
    public class Rectangle //TODO: move this somewhere else
    {
        public ushort x;
        public ushort y;
        public ushort w;
        public ushort h;
        public Encodings.Encodings.Encoding encoding;

        public override string ToString()
        {
            return $"Rectangle: X ({x}), Y ({y}), W ({w}), H ({h}), Encoding ({encoding})";
        }
    }

    public class FramebufferUpdateEvent
    {
        public ushort x;
        public ushort y;
        public ushort w;
        public ushort h;

        public FramebufferUpdateEvent(ushort x, ushort y, ushort w, ushort h)
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
        }
    }

    public class FramebufferUpdate : IVNCMessage
    {
        public ProcessStatus Handle(MessageEvent ev)
        {
            // Message-Type (1) + Padding (1) + NumberOfRectangles (2) + ?*12 >= 4
            if (ev.Data.Length < 4)
                return ProcessStatus.Invalid;

            if (ev.Data[0] != 0) // Message Type 0
                return ProcessStatus.Invalid;

            //TODO: padding check?
            // 1 byte padding
            var numberOfRectangles = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[2..]);
            var end = 4 + numberOfRectangles * 12;
            if (ev.Data.Length < end) //INFO: < cause there should be pixeldata after the rectangle headers
                return ProcessStatus.Invalid;

            var index = 4;
            var rectangles = new List<Rectangle>();
            //TODO: save the current state (i and buffer index) if we need more data but have parsed smth already, so we dont double parse
            for (var i = 0; i < numberOfRectangles; i++)
            {
                if (ev.Data.Length <= index + 12) // header check
                    return ProcessStatus.NeedsMoreBytes;
                // Parse header
                var x = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[index..]);
                var y = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[(index + 2)..]);
                var w = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[(index + 4)..]);
                var h = BinaryPrimitives.ReadUInt16BigEndian(ev.Data[(index + 6)..]);
                var encoding = BinaryPrimitives.ReadInt32BigEndian(ev.Data[(index + 8)..]);
                var rectangle = new Rectangle() { x = x, y = y, w = w, h = h, encoding = (Encodings.Encodings.Encoding)encoding };
                index += 12; // increment past header

                // Try to handle encoding
                if (Encodings.Encodings.Handlers.TryGetValue(rectangle.encoding, out var enc))
                {
                    // only check framebuffer size if we have a known encoding, cause else it could corrupt the buffer
                    CheckFramebufferSize(ev.Connection, x + w, y + h);
                    //TODO: if Format == null: try to guess bpp & endianess (?)
                    var e = new FramebufferUpdateEvent(x, y, w, h);
                    try 
                    { 
                        var status = enc.Parse(ev, e, ref index);
                        if (status == ProcessStatus.NeedsMoreBytes)
                        {
                            //FIXME: also update here cause if we get a huge fuckin update (ie hextile on 4k) we will be stuck here for some time. this could be fixed by requesting more bytes in func or parsing all at once
                            ev.Connection.RaiseFramebufferRefreshEvent();
                            return ProcessStatus.NeedsMoreBytes;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Fail($"Exception during Encoding.Parse: {ex}");
                        //TODO: handle exception during parsing
                        // problem occured, but something probably still changed, so refresh
                        ev.Connection.RaiseFramebufferRefreshEvent();
                        return ProcessStatus.Handled;
                    }

                    // else it was handled
                }
                else
                {
                    Debug.Fail($"Encoding {encoding} not supported");
                    return ProcessStatus.Handled; // stop, so we dont corrupt anything else
                }
                rectangles.Add(rectangle);
            }
            ev.Log($"FramebufferUpdate: Rectangles ({numberOfRectangles}): {string.Join(";", rectangles)}");
            // notify that the framebuffer should be refreshed
            ev.Connection.RaiseFramebufferRefreshEvent();
            return ProcessStatus.Handled;
        }

        public static void CheckFramebufferSize(Connection con, int totalWidth, int totalHeight)
        {
            if (totalWidth > 10000 || totalHeight > 10000) //TODO: make this limit configurable
                return;

            //TODO: only optionally activate?
            // Check if the framebufferupdaterequest is bigger than our current size => resize
            var resizeFound = false;
            if (con.Width == null || totalWidth > con.Width)
            {
                con.Width = (ushort)totalWidth;
                resizeFound = true;
            }
            if (con.Height == null || totalHeight > con.Height)
            {
                con.Height = (ushort)totalWidth;
                resizeFound = true;
            }

            if (resizeFound)
                con.RaiseResizeFramebufferEvent();
        }
    }
}
