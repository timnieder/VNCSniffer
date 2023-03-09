using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VNCSniffer.Core.Messages.Messages;
using VNCSniffer.Core.Messages.Server;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Collections;

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
            return Decode(e.Data, e.Connection.Format, ev, ref index, 16);
        }

        public static ProcessStatus Decode(ReadOnlySpan<byte> data, PixelFormat? format, FramebufferUpdateEvent ev, ref int index, byte tileSize)
        {
            var bpp = format != null ? format.BitsPerPixel : 32;
            bpp /= 8;
            // check if cpixels are smaller
            if (format != null &&
                format.TrueColor &&
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
            var numTiles = numTilesColumn * numTilesRow;
            var tileX = ev.x;
            var tileY = ev.y;
            //TODO: make into pixel array
            ReadOnlySpan<byte> palette = null;
            //TODO: use different palettes for palette and paletteRLE?
            for (var i = 0; i < numTilesRow; i++, tileY += tileSize)
            {
                // the last row can be smaller than 16px high
                var tileH = i == numTilesRow - 1 ? ev.h % tileSize : tileSize;
                for (var j = 0; j < numTilesColumn; j++, tileX += tileSize)
                {
                    // may not have enough bytes for the header
                    if (data.Length < index)
                        return ProcessStatus.NeedsMoreBytes;

                    // last tile in a row can be smaller than 16px wide
                    var tileW = j == numTilesColumn - 1 ? ev.w % tileSize : tileSize;

                    var subencoding = data[index];
                    index++;
                    switch ((SubencodingType)subencoding)
                    {
                        case SubencodingType.Raw: //TODO: just call RawEncoding.Handle here?
                            {
                                var length = tileW * tileH * bpp;
                                if (data.Length < index + length)
                                    return ProcessStatus.NeedsMoreBytes;

                                //TODO: parse bitmap
                                index += length;
                                break;
                            }
                        case SubencodingType.SolidTile: // Solid color tile
                            {
                                if (data.Length < index + bpp)
                                    return ProcessStatus.NeedsMoreBytes;

                                //TODO: parse color
                                var clr = data[index];
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

                                palette = data[(index)..(index + paletteSizeInBytes)];
                                index += paletteSizeInBytes;
                                HandlePackedPixels(data, ref index, tileH, tileW, palette, paletteSize, packedPixelsLength);
                                break;
                            }
                        case SubencodingType.ReusePalette:
                            {
                                // read using palette
                                var paletteSize = (byte)(palette.Length / bpp);
                                var packedPixelsLength = GetPacketPixelsSize(paletteSize, tileW, tileH);
                                if (data.Length < index + packedPixelsLength)
                                    return ProcessStatus.NeedsMoreBytes;

                                HandlePackedPixels(data, ref index, tileH, tileW, palette, paletteSize, packedPixelsLength);
                                break;
                            }
                        case SubencodingType.PlainRLE:
                            {
                                // rle till the tile ends
                                var tilePixels = tileH * tileW;
                                while (tilePixels > 0)
                                {
                                    if (data.Length < index + bpp + 1) // atleast one 
                                        return ProcessStatus.NeedsMoreBytes;

                                    var pixelValue = data[(index)..(index + bpp)];
                                    index += bpp;
                                    var length = 0;
                                    while (data[index] == 255)
                                    {
                                        length += (data[index] + 1); // runLength - 1, so +1
                                        if (data.Length < index + 1) // length not yet done, so we still need data
                                            return ProcessStatus.NeedsMoreBytes;
                                        index++;
                                    }
                                    length += (data[index] + 1); // (runLength-1) mod 255, so +1
                                    index++;
                                    //TODO: draw the run
                                    tilePixels -= length;
                                }
                                break;
                            }
                        case SubencodingType.ReusePaletteRLE:
                            {
                                // read RLE using palette
                                var handled = HandlePaletteRLE(data, ref index, tileH, tileW, palette);
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

                                palette = data[index..(index + paletteSizeInBytes)];
                                index += paletteSizeInBytes;
                                // do paletteRLE
                                var handled = HandlePaletteRLE(data, ref index, tileH, tileW, palette);
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
                }
                tileX = ev.x;
            }
            return ProcessStatus.Handled;
        }

        private static ProcessStatus HandlePaletteRLE(ReadOnlySpan<byte> data, ref int index, int tileH, int tileW, ReadOnlySpan<byte> palette)
        {
            var tilePixels = tileH * tileW;
            while (tilePixels > 0)
            {
                if (data.Length < index + 1) // atleast one index
                    return ProcessStatus.NeedsMoreBytes;

                var paletteIndex = data[index];
                var runLength = 1;
                if (paletteIndex >= 128) // top bit set, run longer than 1
                {
                    paletteIndex -= 128;
                    // parse the run length
                    if (data.Length < index + 1)
                        return ProcessStatus.NeedsMoreBytes;
                    index++;

                    runLength = 0;
                    while (data[index] == 255) //TODO: merge with the one above?
                    {
                        runLength += (data[index] + 1); // runLength - 1, so +1
                        if (data.Length < index + 1) // length not yet done, so we still need data
                            return ProcessStatus.NeedsMoreBytes;
                        index++;
                    }
                    runLength += (data[index] + 1); // (runLength-1) mod 255, so +1
                    // index is increased outside the if
                }
                index++;
                byte clr; //TODO: make clr/pixel
                if (paletteIndex < palette.Length)
                {
                    clr = palette[paletteIndex];
                    // TODO: draw the run
                }
                else
                {
                    Debug.Fail("PaletteIndex out-of-range");
                }
                
                tilePixels -= runLength;
            }
            return ProcessStatus.Handled;
        }

        // Parses packed pixels. Expects the data to be there
        private static void HandlePackedPixels(ReadOnlySpan<byte> data, ref int index, int tileH, int tileW, ReadOnlySpan<byte> palette, byte paletteSize, int packedPixelsLength)
        {
            var packedPixelsBytes = data[(index)..(index + packedPixelsLength)];
            var packedPixels = new BitArray(packedPixelsBytes.ToArray());
            //TODO: how 2 handle bits
            var indexSize = GetPaletteIndexSize(paletteSize);
            for (var y = 0; y < tileH; y++)
            {
                for (var x = 0; x < tileW; x++)
                {
                    //TODO: get palette index
                    //TODO: draw pixel in framebuffer
                }
                var bitCount = (tileW * indexSize);
                if (bitCount % 8 != 0) // skip padding if row isn't a multiple of 8
                {
                    //TODO: skip to next byte
                }
            }
            index += packedPixelsLength;
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
