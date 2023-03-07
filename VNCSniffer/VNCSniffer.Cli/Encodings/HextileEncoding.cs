using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VNCSniffer.Cli.Messages.Messages;
using VNCSniffer.Cli.Messages.Server;
using System.Runtime.CompilerServices;

namespace VNCSniffer.Cli.Encodings
{
    public class HextileEncoding : IEncoding
    {
        [Flags]
        enum SubencodingMask : byte
        {
            Raw = 0b00001,
            BackgroundSpecified = 0b00010,
            ForegroundSpecified = 0b00100,
            AnySubrects = 0b01000,
            SubrectsColored = 0b10000,
        }

        public ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index)
        {
            var bpp = e.Connection.Format != null ? e.Connection.Format.BitsPerPixel : 32;
            bpp /= 8;
            // rectangle is tiled into 16px*16px, so math.ceil(w/16) tiles per row and math.ceil(h/16) tiles per column
            var numTiles = (int)Math.Ceiling(ev.w / 16f) * (int)Math.Ceiling(ev.h / 16f);
            if (e.Data.Length < index + numTiles) // at least one byte per tile
                return ProcessStatus.NeedsMoreBytes;
            //TODO: remove this prev check as its probably not needed?

            //TODO: make this into Pixel/color class?
            ReadOnlySpan<byte> bgColor = null;
            ReadOnlySpan<byte> fgColor = null;
            for (var i = 0; i < numTiles; i++)
            {
                // may not have enough bytes for the header
                if (e.Data.Length < index)
                    return ProcessStatus.NeedsMoreBytes;

                var header = (SubencodingMask)e.Data[index];
                index += 1;

                if (header.HasFlag(SubencodingMask.Raw)) // Raw bytes
                {
                    var length = ev.w * ev.h * bpp;
                    if (e.Data.Length < index + length)
                        return ProcessStatus.NeedsMoreBytes;

                    //TODO: parse bitmap
                    index += length;
                    continue; // other flags are ignored
                }

                // Defines that a tile contains a new background color
                if (header.HasFlag(SubencodingMask.BackgroundSpecified))
                {
                    if (e.Data.Length < index + bpp)
                        return ProcessStatus.NeedsMoreBytes;

                    //TODO: parse bg color
                    bgColor = e.Data[index..];
                    index += bpp;
                }

                // Defines that the tile contains a new foreground color
                if (header.HasFlag(SubencodingMask.ForegroundSpecified))
                {
                    if (e.Data.Length < index + bpp)
                        return ProcessStatus.NeedsMoreBytes;

                    //TODO: parse fg color
                    fgColor = e.Data[index..];
                    index += bpp;
                }

                var numberOfSubrects = 0;
                var subrectLength = 2; // 1 byte x+y, 1 byte w+h
                bool subrectsColored = false;
                // Defines that a tile has subrectangles, followed by the number of subrectangles
                if (header.HasFlag(SubencodingMask.AnySubrects))
                {
                    if (e.Data.Length < index + 1)
                        return ProcessStatus.NeedsMoreBytes;
                    numberOfSubrects = e.Data[index];
                    index += 1;
                }

                // Defines that each subrect in the tale contains a preceding pixel value
                if (header.HasFlag(SubencodingMask.SubrectsColored))
                {
                    if (e.Data.Length < index + 1)
                        return ProcessStatus.NeedsMoreBytes;
                    subrectLength += bpp; // pixel before each subrect
                    subrectsColored = true;
                }

                // length check for all subrects
                if (e.Data.Length < index + (numberOfSubrects * subrectLength))
                    return ProcessStatus.NeedsMoreBytes;

                //TODO: draw bg
                for (var j = 0; j < numberOfSubrects; j++) 
                {
                    var clr = fgColor;
                    if (subrectsColored)
                    {
                        //TODO: read pixel;
                        clr = e.Data[index..];
                        index += bpp;
                    }
                    // xy and wh are merged x and y/w and h values.
                    // The upper 4 bits are x/w and the lower ones being y/h respectively
                    var xy = e.Data[index];
                    var x = xy >> 4;
                    var y = xy & 0b00001111;
                    var wh = e.Data[(index + 1)];
                    var w = wh >> 4;
                    var h = wh & 0b00001111;
                    //TODO: draw subrect
                    index += 2;
                }
            }

            return ProcessStatus.Handled;
        }
    }
}
