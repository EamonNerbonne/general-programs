using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using EmnExtensions.MathHelpers;
using System.Collections;

namespace EmnExtensions.Wpf {
	public static partial class WpfTools {
		public static Color[] MakeDistributedColors(int N, MersenneTwister rnd = null) {
			rnd = rnd ?? RndHelper.ThreadLocalRandom;

			var colors = Enumerable.Range(0, N).Select(i => ColorSimple.Random(rnd)).ToList();

			for (int iter = 0; iter < 2000; iter++) {
				double lr = 0.01 / Math.Sqrt(0.1 * iter + 1);
				for (int i = 0; i < colors.Count; i++) {
#if DEBUG
					ColorSimple old = colors[i];
#endif
					colors[i].RepelFrom(ColorSimple.Random(rnd), lr * 0.01);
					colors[i].RepelFrom(ColorSimple.LightYellow, lr * 0.5);
					colors[i].RepelFrom(ColorSimple.LightGreen, lr * 0.2);
					int other = rnd.Next(colors.Count - 1); //rand other in [0..M-1)
					if (other >= i) other++; //rand other in [0..M) with other != i
					if (N > 1)
						colors[i].RepelFrom(colors[other], lr);
					colors[i].RepelFrom(ColorSimple.MinValue, lr * 0.5);
				}

				ColorSimple min = colors.Aggregate(ColorSimple.MaxValue, ColorSimple.Min);
				ColorSimple max = colors.Aggregate(ColorSimple.MinValue, ColorSimple.Max);

				for (int i = 0; i < colors.Count; i++) colors[i].ScaleBack(min, max, rnd);

			//	if (colors.Count > N) colors.RemoveAt(colors.Count-1);
			}

			return colors.Take(N).Select(c => c.ToWindowsColor()).ToArray();
		}

		struct ColorSimple {
			public static ColorSimple Random(MersenneTwister rnd) {
				return new ColorSimple { R = rnd.NextDouble0To1(), G = rnd.NextDouble0To1(), B = rnd.NextDouble0To1() };
			}
			public double R, G, B;


			public void RepelFrom(ColorSimple other, double lr) {
				double sqrdist = Math.Max(SqrDistTo(other), lr);
				double force = Math.Min(10.0,lr / sqrdist);
				R = R + force * (R - other.R);
				G = G + force * (G - other.G);
				B = B + force * (B - other.B);
			}
			public void ScaleBack(ColorSimple min, ColorSimple max, MersenneTwister rnd) {
				R = scaled(R, min.R, max.R);
				G = scaled(G, min.G, max.G);
				B = scaled(B, min.B, max.B);
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

			public static ColorSimple LightYellow { get { return new ColorSimple { R = 1, G = 1, B = 0.5 }; } }
			public static ColorSimple LightGreen { get { return new ColorSimple { R = 0.5, G = 1, B = 0.5 }; } }


			double SqrDistTo(ColorSimple other) { return 0.7 * sqr(R - other.R) + sqr(G - other.G) + 0.3 * sqr(B - other.B) - 0.2 * sqr(Sum - other.Sum); }
			double Sum { get { return 0.7 * R + G + 0.3 * B; } }

			static double sqr(double x) { return x * x; }
			static double scaled(double val, double min, double max) {
				return max <= min || max < 0.99 && min > 0.01
					? val
					: 0.01 + 0.98 * (val - min) / (max - min);
			}
		}
	}
}
