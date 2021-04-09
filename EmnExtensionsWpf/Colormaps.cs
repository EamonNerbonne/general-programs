using System;
using System.Windows.Media;

namespace EmnExtensions.Wpf
{
    public static class Colormaps
    {
        public static Color Greyscale(double value)
        {
            if (value < 0) {
                value = 0;
            }

            if (value > 1) {
                value = 1;
            }

            var scaled = (byte)(255.9999 * value);
            return Color.FromArgb(255, scaled, scaled, scaled);
        }

        const float dx = 0.8f;

        public static Color Rainbow(double value)
        {
            if (value < 0) {
                value = 0;
            }

            if (value > 1) {
                value = 1;
            }

            value = (6 - 2 * dx) * value + dx;
            var c = Color.FromRgb((byte)(Math.Max(0.0f, (3 - Math.Abs(value - 4) - Math.Abs(value - 5)) / 2) * 255),
                                     (byte)(Math.Max(0.0f, (4 - Math.Abs(value - 2) - Math.Abs(value - 4)) / 2) * 255),
                                     (byte)(Math.Max(0.0f, (3 - Math.Abs(value - 1) - Math.Abs(value - 2)) / 2) * 255));
            /*if (saturation < 1)
                c = ColorConversion.Desaturize(c, saturation);

            if (hueshift > 0 || hueshift < 0)
                c = ColorConversion.HueShift(c, hueshift);*/

            return c;
        }
        public static Color ScaledRainbow(double value)
        {
            if (value < 0) {
                value = 0;
            }

            if (value > 1) {
                value = 1;
            }

            const double rampMargin = 0.05;
            const double ramp1 = 0.29;
            const double ramp2 = 0.71;
            const double hStart = ramp1 - rampMargin;
            const double hEnd = ramp2 + rampMargin;
            var hsl = new HSL {
                S = Math.Min(1.0, (1 - value) / (1 - ramp2)),
                L = Math.Min(value / ramp1, 1.0)
            };
            if (value < hStart) {
                hsl.H = 0;
            } else if (value < hEnd) {
                hsl.H = (value - hStart) / (hEnd - hStart) * 2.0 / 3.0;
            } else {
                hsl.H = 2.0 / 3.0;
            }

            var c = hsl.ToRGB();

            return c;
        }


        public static Color BlueYellow(double value) => Color.FromRgb((byte)((1 - value) * 255),
                         (byte)((1 - value) * 255),
                         (byte)(value * 255));

        public static Color BlueCyanWhite(double value)
        {
            value = value * 3.0;
            var b = Math.Min(1.0, value);
            value = Math.Max(0.0, value - 1.0);
            var g = Math.Min(1.0, value);
            value = Math.Max(0.0, value - 1.0);
            var r = value;
            return Color.FromRgb((byte)(r * 255.9999), (byte)(g * 255.9999), (byte)(b * 255.9999));
        }

        public static Color RedYellowWhite(double value)
        {
            value = value * 3.0;
            var r = Math.Min(1.0, value);
            value = Math.Max(0.0, value - 1.0);
            var g = Math.Min(1.0, value);
            value = Math.Max(0.0, value - 1.0);
            var b = value;
            return Color.FromRgb((byte)(r * 255.9999), (byte)(g * 255.9999), (byte)(b * 255.9999));
        }
        public static Color BlueMagentaWhite(double value)
        {
            value = value * 3.0;
            var b = Math.Min(1.0, value);
            value = Math.Max(0.0, value - 1.0);
            var r = Math.Min(1.0, value);
            value = Math.Max(0.0, value - 1.0);
            var g = value;
            return Color.FromRgb((byte)(r * 255.9999), (byte)(g * 255.9999), (byte)(b * 255.9999));
        }

    }
}
