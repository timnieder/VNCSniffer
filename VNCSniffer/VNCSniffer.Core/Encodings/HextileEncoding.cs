using VNCSniffer.Core.Messages.Server;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Encodings
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
            var format = e.Connection.PixelFormat;
            var bpp = format.BitsPerPixel;
            bpp /= 8;
            // rectangle is tiled into 16px*16px, so math.ceil(w/16) tiles per row and math.ceil(h/16) tiles per column
            var numTilesColumn = (int)Math.Ceiling(ev.w / 16f);
            var numTilesRow = (int)Math.Ceiling(ev.h / 16f);
            var numTiles = numTilesColumn * numTilesRow;
            if (e.Data.Length < index + numTiles) // at least one byte per tile
                return ProcessStatus.NeedsMoreBytes;
            //TODO: remove this prev check as its probably not needed?

            Color? bgColor = null;
            Color? fgColor = null;
            var tileX = ev.x;
            var tileY = ev.y;
            var startRow = 0;
            var startColumn = 0;
            if (e.Connection.lastRectangle != -1)
            {
                startRow = e.Connection.lastRow;
                startColumn = e.Connection.lastColumn;
                bgColor = e.Connection.lastBGColor;
                fgColor = e.Connection.lastFGColor;
                tileY += (ushort)(startRow * 16);
                tileX += (ushort)(startColumn * 16);
            }
            for (var i = startRow; i < numTilesRow; i++, tileY += 16)
            {
                e.Connection.lastRow = i;
                // the last row can be smaller than 16px high
                var tileH = i == numTilesRow - 1 ? (ushort)(ev.h % 16) : (ushort)16;
                for (var j = (i == startRow ? startColumn : 0); j < numTilesColumn; j++, tileX += 16)
                {
                    e.Connection.lastColumn = j;
                    e.Connection.lastIndex = index;
                    // may not have enough bytes for the header
                    if (e.Data.Length < index + 1)
                        return ProcessStatus.NeedsMoreBytes;

                    // last tile in a row can be smaller than 16px wide
                    var tileW = j == numTilesColumn - 1 ? (ushort)(ev.w % 16) : (ushort)16;

                    var header = (SubencodingMask)e.Data[index];
                    index += 1;

                    if (header.HasFlag(SubencodingMask.Raw)) // Raw bytes
                    {
                        var length = tileW * tileH * bpp;
                        if (e.Data.Length < index + length)
                            return ProcessStatus.NeedsMoreBytes;

                        // parse bitmap
                        e.Connection.DrawRegion(e.Data[index..(index + length)], tileX, tileY, tileW, tileH);
                        index += length;
                        continue; // other flags are ignored
                    }

                    // Defines that a tile contains a new background color
                    if (header.HasFlag(SubencodingMask.BackgroundSpecified))
                    {
                        if (e.Data.Length < index + bpp)
                            return ProcessStatus.NeedsMoreBytes;

                        // parse bg color
                        bgColor = new Color(e.Data[index..(index + bpp)], format, bpp, e.Connection.FramebufferPixelFormat);
                        e.Connection.lastBGColor = bgColor;
                        index += bpp;
                    }

                    // Defines that the tile contains a new foreground color
                    if (header.HasFlag(SubencodingMask.ForegroundSpecified))
                    {
                        if (e.Data.Length < index + bpp)
                            return ProcessStatus.NeedsMoreBytes;

                        // parse fg color
                        fgColor = new Color(e.Data[index..(index + bpp)], format, bpp, e.Connection.FramebufferPixelFormat);
                        e.Connection.lastFGColor = fgColor;
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
                        index++;
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

                    // draw bg
                    e.Connection.DrawSolidRect(bgColor.Value, tileX, tileY, tileW, tileH);
                    for (var k = 0; k < numberOfSubrects; k++)
                    {
                        var clr = fgColor;
                        if (subrectsColored)
                        {
                            // Parse subrect color
                            clr = new Color(e.Data[index..(index + bpp)], format, bpp, e.Connection.FramebufferPixelFormat);
                            index += bpp;
                        }
                        //TODO: bigendian check?
                        // xy and wh are merged x and y/w and h values.
                        // The upper 4 bits are x/w and the lower ones being y/h respectively
                        // w and h are width-1 and height-1
                        var xy = e.Data[index];
                        var x = (ushort)(xy >> 4);
                        var y = (ushort)(xy & 0b00001111);
                        var wh = e.Data[(index + 1)];
                        var w = (ushort)((wh >> 4) + 1);
                        var h = (ushort)((wh & 0b00001111) + 1);
                        // draw subrect
                        var absoluteX = (ushort)(tileX + x);
                        var absoluteY = (ushort)(tileY + y);
                        e.Connection.DrawSolidRect(clr.Value, absoluteX, absoluteY, w, h);
                        index += 2;
                    }
                }
                tileX = ev.x;
            }

            return ProcessStatus.Handled;
        }
    }
}
