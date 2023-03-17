using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

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
            // Taken from https://github.com/MarcusWichelmann/MarcusW.VncClient/blob/master/src/MarcusW.VncClient/Protocol/Implementation/PixelConversions.cs#L156
            void Convert(ref byte val, ushort max, ushort dstMax)
            {
                var srcDepth = BitOperations.PopCount(max); //TODO: benchmark against Popcnt.Popcount?
                var dstDepth = BitOperations.PopCount(dstMax);
                // Reduction: Shift the value right so only the most significant bits remain
                if (srcDepth > dstDepth)
                    val >>= (srcDepth - dstDepth);
                // Extension: Shift the value left so the remaining bits get the most significance
                else
                    val <<= (dstDepth - srcDepth);
            }

            //TODO: can we make this better? 8->32bpp, 3->192 instead of 255
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
