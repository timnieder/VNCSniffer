using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace VNCSniffer.Core
{
    public struct Color
    {
        public byte R;
        public byte G;
        public byte B;

        // Parse the 
        public unsafe Color(ReadOnlySpan<byte> bytes, PixelFormat format, byte bpp)
        {
            uint clr;
            //TODO: colormap
            fixed (byte* ptr = bytes)
            {
                switch (bpp)
                {
                    //TODO: let this use binaryprimitve reading directly?
                    // Taken from https://github.com/MarcusWichelmann/MarcusW.VncClient/blob/master/src/MarcusW.VncClient/Protocol/Implementation/PixelConversions.cs#L99
                    case 4:
                    case 3: // we read 4 bytes but ignore the last one
                        var u32 = Unsafe.AsRef<uint>(ptr);
                        if (format.BigEndian)
                            u32 = BinaryPrimitives.ReverseEndianness(u32);
                        clr = u32;
                        break;
                    case 2:
                        var u16 = Unsafe.AsRef<ushort>(ptr);
                        if (format.BigEndian)
                            u16 = BinaryPrimitives.ReverseEndianness(u16);
                        clr = u16;
                        break;
                    case 1:
                        clr = Unsafe.AsRef<byte>(ptr);
                        break;
                    default:
                        Debug.Fail("Invalid PixelFormat.BytesPerPixel");
                        clr = 0;
                        break;
                }
            }

            R = (byte)((clr >> format.RedShift) & format.RedMax);
            G = (byte)((clr >> format.GreenShift) & format.GreenMax);
            B = (byte)((clr >> format.BlueShift) & format.BlueMax);
        }

        // Additionally color correct the value to the destination pixelformat
        public Color(ReadOnlySpan<byte> bytes, PixelFormat format, byte bpp, PixelFormat destFormat) : this(bytes, format, bpp)
        {
            //TODO: try to guess format
            if (destFormat == null)
                return;

            // Taken from https://github.com/MarcusWichelmann/MarcusW.VncClient/blob/master/src/MarcusW.VncClient/Protocol/Implementation/PixelConversions.cs#L156
            void Convert(ref byte val, ushort max, ushort dstMax)
            {
                var srcDepth = BitOperations.PopCount(max); //TODO: benchmark against Popcnt.Popcount?
                var dstDepth = BitOperations.PopCount(dstMax);
                // Reduction: Shift the value right so only the most significant bits remain
                if (srcDepth > dstDepth)
                    val >>= (srcDepth - dstDepth);
                // Extension: Copy the value's bits multiple times; truncate if needed
                else
                {
                    byte newVal = 0;
                    // Start from the left and copy bits to the most left position
                    // After that move srcDepth to the right and repeat
                    for (var i = (dstDepth - srcDepth); true; i -= srcDepth)
                    {
                        // Haven't reached end => copy bits
                        if (i > 0)
                        {
                            newVal |= (byte)(val << i);
                        }
                        else // Reached end, may need to truncate value
                        {
                            var truncate = i * -1;
                            newVal |= (byte)(val >> truncate); // no need to shift left
                            val = newVal;
                            return;
                        }
                    }
                }
            }

            Convert(ref R, format.RedMax, destFormat.RedMax);
            Convert(ref G, format.GreenMax, destFormat.GreenMax);
            Convert(ref B, format.BlueMax, destFormat.BlueMax);
        }

        public static List<Color> ParseList(ReadOnlySpan<byte> bytes, PixelFormat format, byte bpp)
        {
            List<Color> list = new();
            for (var i = 0; i < bytes.Length; i += bpp)
            {
                list.Add(new Color(bytes[i..], format, bpp));
            }
            return list;
        }

        public static List<Color> ParseList(ReadOnlySpan<byte> bytes, PixelFormat format, byte bpp, PixelFormat destFormat)
        {
            List<Color> list = new();
            for (var i = 0; i < bytes.Length; i += bpp)
            {
                list.Add(new Color(bytes[i..], format, bpp, destFormat));
            }
            return list;
        }
    }
}
