using System;
using System.Linq;
using System.Windows.Media;
using EmnExtensions.MathHelpers;

namespace EmnExtensions.Wpf {
	public static partial class WpfTools {
		public static Color[] MakeDistributedColors(int N, MersenneTwister rnd = null) {
			rnd = rnd ?? RndHelper.ThreadLocalRandom;

			var colors = Enumerable.Range(0, N).Select(i => ColorSimple.Random(rnd)).ToArray();
			if (colors.Length > 1)
				for (int iter = 0; iter < 10 + 100000/(N*N); iter++) {
					double lr = 1.0 / Math.Sqrt(iter*N*N + 1000);
					for (int i = 0; i < colors.Length; i++) {
						colors[i].RepelFrom(ColorSimple.MaxValue, 0.07 * lr);
						colors[i].RepelFrom(ColorSimple.LightYellow, 0.03 * lr);
						colors[i].RepelFrom(ColorSimple.MinValue, 0.1 * lr);
						colors[i].RepelFrom(ColorSimple.Random(rnd), lr * lr);
						for (int j = 0; j < colors.Length; j++) 
							if (i != j) colors[i].RepelFrom(colors[j], lr);
					}

					ColorSimple min = colors.Aggregate(ColorSimple.MinValue, ColorSimple.Min);
					ColorSimple max = colors.Aggregate(ColorSimple.MaxValue, ColorSimple.Max);
					for (int i = 0; i < colors.Length; i++) { colors[i].ScaleBack(min, max); colors[i].LimitBrightness(0.85); }
				}

			return colors.Select(c => c.ToWindowsColor()).ToArray();
		}

		struct ColorSimple {
			public static ColorSimple Random(MersenneTwister rnd) {
				return new ColorSimple { R = rnd.NextDouble0To1(), G = rnd.NextDouble0To1(), B = rnd.NextDouble0To1() };
			}
			double R, G, B;


			public void RepelFrom(ColorSimple other, double lr) {
				double sqrdist = Math.Max(SqrDistTo(other), lr/100);
				double force =  lr / sqrdist;
				R = R + force * (R - other.R);
				G = G + force * (G - other.G);
				B = B + force * (B - other.B);
			}
			public void ScaleBack(ColorSimple min, ColorSimple max) {
				R = scaled(R, min.R, max.R);
				G = scaled(G, min.G, max.G);
				B = scaled(B, min.B, max.B);
			}

			public void LimitBrightness(double max) {
				if (Sum > max) {
					double scale = max / Sum;
					R *= scale;
					G *= scale;
					B *= scale;
				}
			}



			public Color ToWindowsColor() {
				return Color.FromRgb((byte)(255 * R + 0.5), (byte)(255 * G + 0.5), (byte)(255 * B + 0.5));
			}

			public static ColorSimple Min(ColorSimple a, ColorSimple b) {
				return new ColorSimple { R = Math.Min(a.R, b.R), G = Math.Min(a.G, b.G), B = Math.Min(a.B, b.B) };
			}

			public static ColorSimple Max(ColorSimple a, ColorSimple b) {
				return new ColorSimple { R = Math.Max(a.R, b.R), G = Math.Max(a.G, b.G), B = Math.Max(a.B, b.B) };
			}

			public static ColorSimple MaxValue { get { return new ColorSimple { R = 1.0, G = 1.0, B = 1.0 }; } }
			public static ColorSimple MinValue { get { return new ColorSimple { R = 0.0, G = 0.0, B = 0.0 }; } }

			public static ColorSimple LightYellow { get { return new ColorSimple { R = 1, G = 1, B = 0.75 }; } }
			public static ColorSimple LightGreen { get { return new ColorSimple { R = 0.5, G = 1, B = 0.5 }; } }
			public static ColorSimple Grey { get { return new ColorSimple { R = 0.5, G = 0.5, B = 0.5 }; } }


			public double SqrDistTo(ColorSimple other) { return (sqr(R - other.R) + sqr(G - other.G) + sqr(B - other.B)) / 3.0 - 0.5 * HueEmphasis * sqr(Sum - other.Sum); }
			double Sum { get { return 0.35 * R + 0.5 * G + 0.15 * B; } }

			static double sqr(double x) { return x * x; }
			static double scaled(double val, double min, double max) {
				return max <= min || max < 0.99 && min > 0.01
					? val
					: 0.01 + 0.98 * (val - min) / (max - min);
			}
			const double HueEmphasis = 1.0;
		}
	}
}
