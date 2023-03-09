using System.Buffers.Binary;
using System.Drawing;

namespace VNCSniffer.Core
{
    public class PixelFormat
    {
        public byte BitsPerPixel; // 0
        public byte Depth; // 1
        public bool BigEndian; // 2
        public bool TrueColor; // 3
        public ushort RedMax; // 4
        public ushort GreenMax; // 6
        public ushort BlueMax; // 8
        public byte RedShift; // 10
        public byte GreenShift; // 11
        public byte BlueShift; // 12
        // padding // 13
        // = 16

        public PixelFormat() { }

        public PixelFormat(byte bpp, byte depth, bool bigEndian, bool trueColor, ushort redMax, ushort greenMax, ushort blueMax, byte redShift, byte greenShift, byte blueShift)
        {
            BitsPerPixel = bpp;
            Depth = depth;
            BigEndian = bigEndian;
            TrueColor = trueColor;
            RedMax = redMax;
            GreenMax = greenMax;
            BlueMax = blueMax;
            RedShift = redShift;
            GreenShift = greenShift;
            BlueShift = blueShift;
        }

        public PixelFormat(ReadOnlySpan<byte> bytes)
        {
            BitsPerPixel = bytes[0];
            Depth = bytes[1];
            BigEndian = BitConverter.ToBoolean(bytes[2..3]);
            TrueColor = BitConverter.ToBoolean(bytes[3..4]);
            RedMax = BinaryPrimitives.ReadUInt16BigEndian(bytes[4..6]);
            GreenMax = BinaryPrimitives.ReadUInt16BigEndian(bytes[6..8]);
            BlueMax = BinaryPrimitives.ReadUInt16BigEndian(bytes[8..10]);
            RedShift = bytes[10];
            GreenShift = bytes[11];
            BlueShift = bytes[12];
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PixelFormat))
                return base.Equals(obj);

            return Equals((PixelFormat)obj);
        }

        public bool Equals(PixelFormat obj)
        {
            return BitsPerPixel == obj.BitsPerPixel
                && Depth == obj.Depth
                && BigEndian == obj.BigEndian
                && TrueColor == obj.TrueColor
                && RedMax == obj.RedMax
                && GreenMax == obj.GreenMax
                && BlueMax == obj.BlueMax
                && RedShift == obj.RedShift
                && GreenShift == obj.GreenShift
                && BlueShift == obj.BlueShift;
        }
    }
}
