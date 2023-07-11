using Sewer56.BitStream;
using Sewer56.BitStream.ByteStreams;
using System.Buffers.Binary;
using System.Diagnostics;
using VNCSniffer.Core.Messages.Server;
using static VNCSniffer.Core.Messages.Messages;

namespace VNCSniffer.Core.Encodings
{
    public class TRLEEncoding : IEncoding
    {
        enum SubencodingType : byte
        {
            Raw = 0,
            SolidTile = 1,
            PaletteStart = 2,
            PaletteEnd = 16,
            ReusePalette = 127,
            PlainRLE = 128,
            ReusePaletteRLE = 129,
            PaletteRLEStart = 130,
            PaletteRLEEnd = 255,
        }
        public ProcessStatus Parse(MessageEvent e, FramebufferUpdateEvent ev, ref int index)
        {
            return Decode(e.Data, e.Connection, ev, ref index, 16);
        }

        public static ProcessStatus Decode(ReadOnlySpan<byte> data, Connection connection, FramebufferUpdateEvent ev, ref int index, byte tileSize)
        {
            var format = connection.PixelFormat;
            var bpp = format.BitsPerPixel;
            bpp /= 8;
            // check if cpixels are smaller
            if (format.TrueColor &&
                format.BitsPerPixel == 32)
            {
                // Check if we can fit all color bits into 3 bytes
                var maxValue = format.RedMax << format.RedShift |
                    format.GreenMax << format.GreenShift |
                    format.BlueMax << format.BlueShift;
                if (format.BigEndian) //TODO: check if this is right?
                    maxValue = BinaryPrimitives.ReverseEndianness(maxValue);
                if ((maxValue & 0xFF000000) == 0) // higher bits empty?
                    bpp = 3;
            }
            // rectangle is tiled into 16px*16px, so math.ceil(w/16) tiles per row and math.ceil(h/16) tiles per column
            var numTilesColumn = (int)Math.Ceiling(ev.w / (float)tileSize);
            var numTilesRow = (int)Math.Ceiling(ev.h / (float)tileSize);
            var tileX = ev.x;
            var tileY = ev.y;
            //TODO: make into pixel array
            List<Color> palette = null;
            //TODO: use different palettes for palette and paletteRLE?
            var lastEncoding = -1;
            for (var i = 0; i < numTilesRow; i++, tileY += tileSize)
            {
                // the last row can be smaller than 16px high
                ushort tileH = tileSize;
                if (i == numTilesRow - 1)
                {
                    var remainder = (ushort)(ev.h % tileSize);
                    if (remainder != 0)
                        tileH = remainder;
                }
                for (var j = 0; j < numTilesColumn; j++, tileX += tileSize)
                {
                    // may not have enough bytes for the header
                    if (data.Length < index + 1)
                        return ProcessStatus.NeedsMoreBytes;

                    // last tile in a row can be smaller than 16px wide
                    ushort tileW = tileSize;
                    if (j == numTilesColumn - 1)
                    {
                        var remainder = (ushort)(ev.w % tileSize);
                        if (remainder != 0)
                            tileW = remainder;
                    }

                    var subencoding = data[index];
                    index++;
                    switch ((SubencodingType)subencoding)
                    {
                        case SubencodingType.Raw: //TODO: just call RawEncoding.Handle here?
                            {
                                var length = tileW * tileH * bpp;
                                if (data.Length < index + length)
                                    return ProcessStatus.NeedsMoreBytes;

                                // parse bitmap
                                connection.DrawRegion(data[index..(index + length)], tileX, tileY, tileW, tileH, bpp);
                                index += length;
                                break;
                            }
                        case SubencodingType.SolidTile: // Solid color tile
                            {
                                if (data.Length < index + bpp)
                                    return ProcessStatus.NeedsMoreBytes;

                                //TODO: parse color
                                var clr = new Color(data[index..(index + bpp)], format, bpp, connection.FramebufferPixelFormat);
                                // draw tile
                                connection.DrawSolidRect(clr, tileX, tileY, tileW, tileH);
                                index += bpp;
                                break;
                            }
                        case SubencodingType t when t >= SubencodingType.PaletteStart && t <= SubencodingType.PaletteEnd: // Packed palette
                            {
                                // read palette
                                var paletteSize = (byte)t;
                                var paletteSizeInBytes = paletteSize * bpp;
                                var packedPixelsLength = GetPacketPixelsSize(paletteSize, tileW, tileH);
                                if (data.Length < index + paletteSizeInBytes + packedPixelsLength)
                                    return ProcessStatus.NeedsMoreBytes;

                                palette = Color.ParseList(data[(index)..(index + paletteSizeInBytes)], format, bpp, connection.FramebufferPixelFormat);
                                index += paletteSizeInBytes;
                                HandlePackedPixels(data, ref index, connection, tileX, tileY, tileH, tileW, palette, paletteSize, bpp, packedPixelsLength);
                                break;
                            }
                        case SubencodingType.ReusePalette:
                            {
                                // ZRLE check
                                Debug.Assert(tileSize != 64);
                                // read using palette
                                if (palette == null)
                                    throw new Exception("Palette used before setting."); //TODO: make fail safe
                                var paletteSize = (byte)palette.Count;
                                var packedPixelsLength = GetPacketPixelsSize(paletteSize, tileW, tileH);
                                if (data.Length < index + packedPixelsLength)
                                    return ProcessStatus.NeedsMoreBytes;

                                HandlePackedPixels(data, ref index, connection, tileX, tileY, tileH, tileW, palette, paletteSize, bpp, packedPixelsLength);
                                break;
                            }
                        case SubencodingType.PlainRLE:
                            {
                                // rle till the tile ends
                                var tilePixels = tileH * tileW;
                                var curXOffset = 0;
                                var curYOffset = 0;
                                while (tilePixels > 0)
                                {
                                    if (data.Length < index + bpp + 1) // atleast one 
                                        return ProcessStatus.NeedsMoreBytes;

                                    var pixelValue = new Color(data[index..(index + bpp)], format, bpp, connection.FramebufferPixelFormat);
                                    index += bpp;
                                    var length = 0;
                                    while (data[index] == 255)
                                    {
                                        length += data[index]; // 255
                                        if (data.Length < index + 1) // length not yet done, so we still need data
                                            return ProcessStatus.NeedsMoreBytes;
                                        index++;
                                    }
                                    length += (data[index] + 1); // (runLength-1) mod 255, so +1
                                    index++;
                                    // draw the run
                                    var toWrite = length;
                                    while (toWrite > 0) // draw until no more left to write
                                    {
                                        var cur = toWrite;
                                        var leftInRow = tileW - curXOffset;
                                        if (toWrite > leftInRow) // can only draw so many pixels in this row, max 16
                                            cur = leftInRow;
                                        connection.DrawPixel(pixelValue, (ushort)(tileX + curXOffset), (ushort)(tileY + curYOffset), (ushort)cur);
                                        toWrite -= cur;
                                        // offset the cursor by the length we've written
                                        curXOffset += cur;
                                        if (curXOffset >= tileW) // if we've written a line, go to the beginning of the next line
                                        {
                                            curXOffset = 0;
                                            curYOffset += 1;
                                        }
                                    }
                                    tilePixels -= length;
                                }
                                break;
                            }
                        case SubencodingType.ReusePaletteRLE:
                            {
                                // ZRLE check
                                Debug.Assert(tileSize != 64);
                                // read RLE using palette
                                var handled = HandlePaletteRLE(data, ref index, connection, tileX, tileY, tileH, tileW, palette, bpp);
                                if (handled == ProcessStatus.NeedsMoreBytes)
                                    return handled;
                                break;
                            }
                        case SubencodingType t when t >= SubencodingType.PaletteRLEStart && t <= SubencodingType.PaletteRLEEnd:
                            {
                                // read palette
                                var paletteSize = (byte)t - 128;
                                var paletteSizeInBytes = paletteSize * bpp;
                                if (data.Length < index + paletteSizeInBytes)
                                    return ProcessStatus.NeedsMoreBytes;

                                palette = Color.ParseList(data[index..(index + paletteSizeInBytes)], format, bpp, connection.FramebufferPixelFormat);
                                index += paletteSizeInBytes;
                                // do paletteRLE
                                var handled = HandlePaletteRLE(data, ref index, connection, tileX, tileY, tileH, tileW, palette, bpp);
                                if (handled == ProcessStatus.NeedsMoreBytes)
                                    return handled;
                                break;
                            }
                        default:
                            {
                                Debug.Fail($"Unknown TRLE Subencoding {subencoding}");
                                break;
                            }
                    }
                    //Console.WriteLine($"Subencoding: {(SubencodingType)subencoding} ({subencoding})");
                    lastEncoding = subencoding;
                }
                tileX = ev.x;
            }
            return ProcessStatus.Handled;
        }

        private static ProcessStatus HandlePaletteRLE(ReadOnlySpan<byte> data, ref int index, Connection connection, int tileX, int tileY, int tileH, int tileW, List<Color> palette, int bpp)
        {
            var tilePixels = tileH * tileW;
            var curXOffset = 0;
            var curYOffset = 0;
            while (tilePixels > 0)
            {
                if (data.Length < index + 1) // atleast one index
                    return ProcessStatus.NeedsMoreBytes;

                var paletteIndex = data[index];
                var runLength = 1;
                if (paletteIndex >= 128) // top bit set, run longer than 1
                {
                    paletteIndex -= 128;
                    index++;
                    // parse the run length
                    if (data.Length < index + 1)
                        return ProcessStatus.NeedsMoreBytes;

                    runLength = 0;
                    while (data[index] == 255) //TODO: merge with the one above?
                    {
                        runLength += data[index]; // 255
                        if (data.Length < index + 1) // length not yet done, so we still need data
                            return ProcessStatus.NeedsMoreBytes;
                        index++;
                    }
                    runLength += (data[index] + 1); // (runLength-1) mod 255, so +1
                    // index is increased outside the if
                }
                index++;
                var clr = GetColorFromPalette(paletteIndex, palette, bpp);
                if (clr != null)
                {
                    // draw the run
                    var toWrite = runLength;
                    while (toWrite > 0) // draw until no more left to write
                    {
                        var cur = toWrite;
                        var leftInRow = tileW - curXOffset;
                        if (toWrite > leftInRow) // can only draw so many pixels in this row, max 16
                            cur = leftInRow;
                        connection.DrawPixel(clr.Value, (ushort)(tileX + curXOffset), (ushort)(tileY + curYOffset), (ushort)cur);
                        toWrite -= cur;
                        // offset the cursor by the length we've written
                        curXOffset += cur;
                        if (curXOffset >= tileW) // if we've written a line, go to the beginning of the next line
                        {
                            curXOffset = 0;
                            curYOffset += 1;
                        }
                    }
                }

                tilePixels -= runLength;
            }
            return ProcessStatus.Handled;
        }

        // Parses packed pixels. Expects the data to be there
        private static void HandlePackedPixels(ReadOnlySpan<byte> data, ref int index, Connection connection, ushort tileX, ushort tileY, int tileH, int tileW, List<Color> palette, byte paletteSize, int bpp, int packedPixelsLength)
        {
            var packedPixelsBytes = data[(index)..(index + packedPixelsLength)];
            //TODO: make own IByteStream implementation for readonlyspans to avoid copying?
            var stream = new ArrayByteStream(packedPixelsBytes.ToArray());
            var packedPixels = new BitStream<ArrayByteStream>(stream);
            var indexSize = GetPaletteIndexSize(paletteSize);
            for (var y = 0; y < tileH; y++)
            {
                for (var x = 0; x < tileW; x++)
                {
                    // get palette index
                    var paletteIndex = packedPixels.Read<byte>(indexSize);
                    var clr = GetColorFromPalette(paletteIndex, palette, bpp); //TODO: make clr/pixel
                    // draw pixel in framebuffer
                    if (clr != null)
                        connection.DrawPixel(clr.Value, (ushort)(tileX + x), (ushort)(tileY + y));
                }
                var bitCount = (tileW * indexSize);
                var missingBits = (byte)(bitCount % 8);
                if (missingBits != 0) // skip padding if row isn't a multiple of 8
                {
                    // skip to next byte
                    packedPixels.SeekRelative(0, missingBits);
                }
            }
            index += packedPixelsLength;
        }

        public static Color? GetColorFromPalette(byte paletteIndex, List<Color> palette, int bpp)
        {
            if (paletteIndex < palette.Count)
            {
                return palette[paletteIndex];
            }
            else
            {
                Debug.Fail("PaletteIndex out-of-range");
                return null;
            }
        }

        // Returns the size of the packed pixels according to the palette and tile size
        public static int GetPacketPixelsSize(byte paletteSize, int w, int h) => paletteSize switch
        {
            2 => (w + 7) / 8 * h, // (width+7)/8 * height
            >= 3 and <= 4 => (w + 3) / 4 * h, // (width+3)/4 * height
            >= 5 and <= 16 => (w + 1) / 2 * h, // (width+1)/2 * height
            _ => throw new Exception("Invalid PaletteSize"),
        };

        // Returns the size of the palette index in bits
        public static int GetPaletteIndexSize(byte paletteSize) => paletteSize switch
        {
            2 => 1,
            >= 3 and <= 4 => 2,
            >= 5 and <= 16 => 4,
            _ => throw new Exception("Invalid PaletteSize"),
        };
    }
}
