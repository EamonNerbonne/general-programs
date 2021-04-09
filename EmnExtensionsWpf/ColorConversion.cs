// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
using System.Windows.Media;

//Loosely based on a version originally by Marten Veldthuis, with modifications by Eamon Nerbonne.
namespace EmnExtensions.Wpf
{
    public struct HSL
    {
        double _h;
        double _s;
        double _l;
        public double H { get { return _h; } set { _h = value % 1.0; } }
        public double S { get { return _s; } set { _s = value > 1.0 ? 1.0 : value < 0.0 ? 0.0 : value; } }
        public double L { get { return _l; } set { _l = value > 1.0 ? 1.0 : value < 0.0 ? 0.0 : value; } }

        public HSL(Color c)
        {
            var stats = new ColorStats(c);
            _l = stats.LuminenceMax / 255.0;
            _s = stats.LuminenceMax == 0 ? 0.0 : stats.LuminenceRange / (double)stats.LuminenceMax; // Protecting from the impossible operation of division by zero.
            _h = stats.LuminenceRange == 0 ? 0.0 : (stats.PrimaryColorOffset / 3.0 + stats.SecondaryChannelsDiff / 6.0 / stats.LuminenceRange) % 1.0;
        }

        struct ColorStats
        {
            public readonly int LuminenceMax, LuminenceRange, PrimaryColorOffset, SecondaryChannelsDiff;
            public ColorStats(Color c)
            {
                int LuminenceMin;
                if (c.R > c.G) {
                    LuminenceMax = c.R;
                    LuminenceMin = c.G;
                    PrimaryColorOffset = 3; //mathematically equivalent to 0, but we use 3 to enable modulo wrap-around without negative number issues.
                    SecondaryChannelsDiff = c.G - c.B;
                } else {
                    LuminenceMax = c.G;
                    LuminenceMin = c.R;
                    PrimaryColorOffset = 1;
                    SecondaryChannelsDiff = c.B - c.R;
                }

                if (c.B > LuminenceMax) {
                    LuminenceMax = c.B;
                    PrimaryColorOffset = 2;
                    SecondaryChannelsDiff = c.R - c.G;
                } else if (c.B < LuminenceMin) {
                    LuminenceMin = c.B;
                }
                LuminenceRange = LuminenceMax - LuminenceMin;
            }
        }

        public Color ToRGB()
        {
            var Max = RoundToByte(L * 255);
            var Min = RoundToByte((1.0 - S) * (L / 1.0) * 255);
            double q = Max - Min;

            var H6 = H * 6;

            if (H6 <= 1.0) {
                var Mid = RoundToByte((H6 - 0) * q + Min);
                return Color.FromRgb(Max, Mid, Min);
            } else if (H6 <= 2.0) {
                var Mid = RoundToByte(-(H6 - 1.0) * q + Max);
                return Color.FromRgb(Mid, Max, Min);
            } else if (H6 <= 3.0) {
                var Mid = RoundToByte((H6 - 2.0) * q + Min);
                return Color.FromRgb(Min, Max, Mid);
            } else if (H6 <= 4.0) {
                var Mid = RoundToByte(-(H - 3.0) * q + Max);
                return Color.FromRgb(Min, Mid, Max);
            } else if (H6 <= 5.0) {
                var Mid = RoundToByte((H6 - 4.0) * q + Min);
                return Color.FromRgb(Mid, Min, Max);
            } else if (H6 <= 6.0) {
                var Mid = RoundToByte(-(H6 - 5.0) * q + Max);
                return Color.FromRgb(Max, Min, Mid);
            } else //???? should never happen.
                return Color.FromRgb(0, 0, 0);
        }

        static byte RoundToByte(double d) { return (byte)(d + 0.5); }

        public static Color Desaturize(Color c, double saturation)
        {
            return new HSL(c) { S = saturation }.ToRGB();
        }

        public static Color HueShift(Color c, double shift)
        {
            var hsl = new HSL(c);
            hsl.H += shift;
            return hsl.ToRGB();
        }
    }
}
