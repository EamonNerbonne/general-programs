using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

//originally by Marten Veldthuis, with minor adaptations.
namespace EmnExtensions.Wpf
{

    public class HSL {
        public HSL() {
            _h = 0;
            _s = 0;
            _l = 0;
        }

        double _h;
        double _s;
        double _l;

        public double H {
            get { return _h; }
            set {
                _h = value;
                _h = _h % 1;
            }
        }

        public double S {
            get { return _s; }
            set {
                _s = value;
                _s = _s > 1 ? 1 : _s < 0 ? 0 : _s;
            }
        }

        public double L {
            get { return _l; }
            set {
                _l = value;
                _l = _l > 1 ? 1 : _l < 0 ? 0 : _l;
            }
        }
    } 

    public static class ColorConversion {

        /// <summary> 
        /// Converts RGB to HSL 
        /// </summary> 
        /// <param name="c">A Color to convert</param> 
        /// <returns>An HSL value</returns> 
        public static HSL RGB_to_HSL(Color c) {
            HSL hsl =  new HSL();

            int Max, Min, Diff, Sum;
            // Of our RGB values, assign the highest value to Max, and the Smallest to Min
            if (c.R > c.G) { Max = c.R; Min = c.G; } else { Max = c.G; Min = c.R; }
            if (c.B > Max)
                Max = c.B;
            else if (c.B < Min)
                Min = c.B;
            Diff = Max - Min;
            Sum = Max + Min;
            // Luminance - a.k.a. Brightness - Adobe photoshop uses the logic that the
            // site VBspeed regards (regarded) as too primitive = superior decides the 
            // level of brightness.
            hsl.L = (double)Max / 255;
            // Saturation
            if (Max == 0)
                hsl.S = 0; // Protecting from the impossible operation of division by zero.
            else
                hsl.S = (double)Diff / Max; // The logic of Adobe Photoshops is this simple.
            // Hue  R is situated at the angel of 360 eller noll degrees; 
            //   G vid 120 degrees
            //   B vid 240 degrees
            double q;
            if (Diff == 0)
                q = 0; // Protecting from the impossible operation of division by zero.
            else
                q = (double)60 / Diff;

            if (Max == c.R) {
                if (c.G < c.B)
                    hsl.H = (double)(360 + q * (c.G - c.B)) / 360;
                else
                    hsl.H = (double)(q * (c.G - c.B)) / 360;
            } else if (Max == c.G)
                hsl.H = (double)(120 + q * (c.B - c.R)) / 360;
            else if (Max == c.B)
                hsl.H = (double)(240 + q * (c.R - c.G)) / 360;
            else
                hsl.H = 0.0;
            return hsl;
        }

		private static byte Round(double d) {
			return (byte)(d + 0.5);
		}
        /// <summary> 
        /// Converts a colour from HSL to RGB 
        /// </summary> 
        /// <remarks>Adapted from the algoritm in Foley and Van-Dam</remarks> 
        /// <param name="hsl">The HSL value</param> 
        /// <returns>A Color structure containing the equivalent RGB values</returns> 
        public static Color HSL_to_RGB(HSL hsl) {
            byte Max, Mid, Min;
            double q;

            Max = Round(hsl.L * 255);
            Min = Round((1.0 - hsl.S) * (hsl.L / 1.0) * 255);
            q = (double)(Max - Min) / 255;

            if (hsl.H >= 0 && hsl.H <= (double)1 / 6) {
                Mid = Round(((hsl.H - 0) * q) * 1530 + Min);
                return Color.FromRgb(Max, Mid, Min);
            } else if (hsl.H <= (double)1 / 3) {
                Mid = Round(-((hsl.H - (double)1 / 6) * q) * 1530 + Max);
                return Color.FromRgb(Mid, Max, Min);
            } else if (hsl.H <= 0.5) {
                Mid = Round(((hsl.H - (double)1 / 3) * q) * 1530 + Min);
                return Color.FromRgb(Min, Max, Mid);
            } else if (hsl.H <= (double)2 / 3) {
                Mid = Round(-((hsl.H - 0.5) * q) * 1530 + Max);
                return Color.FromRgb(Min, Mid, Max);
            } else if (hsl.H <= (double)5 / 6) {
                Mid = Round(((hsl.H - (double)2 / 3) * q) * 1530 + Min);
                return Color.FromRgb(Mid, Min, Max);
            } else if (hsl.H <= 1.0) {
                Mid = Round(-((hsl.H - (double)5 / 6) * q) * 1530 + Max);
                return Color.FromRgb(Max, Min, Mid);
            } else
                return Color.FromRgb(0, 0, 0);
        }

        public static Color Desaturize(Color c, double saturation) {
            HSL hsl = RGB_to_HSL(c);
            hsl.S = saturation;
            return HSL_to_RGB(hsl);
        }

        public static Color HueShift(Color c, double shift) {
            HSL hsl = RGB_to_HSL(c);
            hsl.H += shift;
            return HSL_to_RGB(hsl); 
        }

    }


}
