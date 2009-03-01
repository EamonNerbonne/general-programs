using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace EmnExtensions.Wpf
{
	public static class Colormaps
	{
		public static Color Greyscale(double value) {
			if (value < 0) value = 0; if (value > 1) value = 1;
			byte scaled = (byte)(255.9999 * value);
			return Color.FromArgb(255, scaled, scaled, scaled);
		}

		const float dx = 0.8f;
		const double saturation = 1.0d;     // TODO make this a user setting in the interface
		const double hueshift = 0.0d;       // TODO make this a user setting in the interface
		public static Color Rainbow(double value) {
			if (value < 0) value = 0; if (value > 1) value = 1;
			value = (6 - 2 * dx) * value + dx;
			Color c = Color.FromRgb((byte)(Math.Max(0.0f, (3 - Math.Abs(value - 4) - Math.Abs(value - 5)) / 2) * 255),
									 (byte)(Math.Max(0.0f, (4 - Math.Abs(value - 2) - Math.Abs(value - 4)) / 2) * 255),
									 (byte)(Math.Max(0.0f, (3 - Math.Abs(value - 1) - Math.Abs(value - 2)) / 2) * 255));
			/*if (saturation < 1)
				c = ColorConversion.Desaturize(c, saturation);

			if (hueshift > 0 || hueshift < 0)
				c = ColorConversion.HueShift(c, hueshift);*/

			return c;
		}


		public static Color BlueYellow(double value) {
			return Color.FromRgb((byte)((1 - value) * 255),
						 (byte)((1 - value) * 255),
						 (byte)(value * 255));

		}

		public static Color BlueCyanWhite(double value) {
			value = value * 3.0;
			double b = Math.Min(1.0, value);
			value = Math.Max(0.0, value - 1.0);
			double g = Math.Min(1.0, value);
			value = Math.Max(0.0, value - 1.0);
			double r = value;
			return Color.FromRgb((byte)(r * 255.9999), (byte)(g * 255.9999), (byte)(b * 255.9999));
		}
		
		public static Color RedYellowWhite(double value) {
			value = value * 3.0;
			double r = Math.Min(1.0, value);
			value = Math.Max(0.0, value - 1.0);
			double g = Math.Min(1.0, value);
			value = Math.Max(0.0, value - 1.0);
			double b = value;
			return Color.FromRgb((byte)(r * 255.9999), (byte)(g * 255.9999), (byte)(b * 255.9999));
		}
		public static Color BlueMagentaWhite(double value) {
			//value = Math.Pow(value, 1.1) * 3.0;
			double b = Math.Min(0.4, value)/0.4;
			value = Math.Max(0.0, value - 0.4);
			
			double r = Math.Min(0.3, value)/0.3;
			value = Math.Max(0.0, value - 0.3);
			
			double g = value/0.3;
			return Color.FromRgb((byte)(r * 255.9999), (byte)(g * 255.9999), (byte)(b * 255.9999));
		}

	}
}
