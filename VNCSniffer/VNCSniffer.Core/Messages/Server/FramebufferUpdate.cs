﻿using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Messages.Server
{
    public class Rectangle //TODO: move this somewhere else
    {
        public ushort x;
        public ushort y;
        public ushort w;
        public ushort h;
        public int encoding;

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
                index += 12;
                if (Encodings.Encodings.Handlers.TryGetValue(encoding, out var enc))
                {
                    var e = new FramebufferUpdateEvent(x, y, w, h);
                    var status = enc.Parse(ev, e, ref index);
                    if (status == ProcessStatus.NeedsMoreBytes)
                        return ProcessStatus.NeedsMoreBytes;

                    // else it was handled
                }
                else
                {
                    Console.WriteLine($"Encoding {encoding} not supported"); //TODO: can we do anything else?
                    //TODO: break? fallthrough and assume length is 0?
                }
                rectangles.Add(new Rectangle() { x = x, y = y, w = w, h = h, encoding = encoding });
            }
            ev.Log($"FramebufferUpdate: Rectangles ({numberOfRectangles}): {string.Join(";", rectangles)}");
            return ProcessStatus.Handled;
        }
    }
}