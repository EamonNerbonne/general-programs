using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HwrSplitter.Engine
{
    public struct PixelArgb32 {
        public uint Data;

        public const uint AMask = 0xff000000u;
        public const int AShift = 24;
        public const uint RMask = 0x00ff0000u;
        public const int RShift = 16;
        public const uint GMask = 0x0000ff00u;
        public const int GShift = 8;
        public const uint BMask = 0x000000ffu;
        public const int BShift = 0;

        public byte A {
            get { return (byte)((AMask & Data) >> AShift); }
            set { Data = (Data & ~AMask) | (((uint)value) << AShift); }
        }
        public byte R {
            get { return (byte)((RMask & Data) >> RShift); }
            set { Data = (Data & ~RMask) | (((uint)value) << RShift); }
        }
        public byte G {
            get { return (byte)((GMask & Data) >> GShift); }
            set { Data = (Data & ~GMask) | (((uint)value) << GShift); }
        }
        public byte B {
            get { return (byte)((BMask & Data) >> BShift); }
            set { Data = (Data & ~BMask) | (((uint)value) << BShift); }
        }

        public PixelArgb32(byte a, byte r, byte g, byte b) {
            Data = (((uint)a) << AShift) | (((uint)r) << RShift) | (((uint)g) << GShift) | (((uint)b) << BShift);
        }

        public static PixelArgb32 Combine(PixelArgb32 x, PixelArgb32 y,Func<byte,byte,byte> func) {
            return new PixelArgb32(func(x.A, y.A), func(x.R, y.R), func(x.G, y.G), func(x.B, y.B));
        }
		public static implicit operator PixelArgb32(int raw) {
			return new PixelArgb32 { Data = (uint)raw };
		}
		public static implicit operator PixelArgb32(uint raw) {
			return new PixelArgb32 { Data = raw };
		}
	}
}
