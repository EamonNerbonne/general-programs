using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using EmnExtensions.MathHelpers;
using System.Collections;

namespace EmnExtensions.Wpf.OldGraph
{
	public static class GraphRandomPen
	{
		static Random GraphColorRandom = new EmnExtensions.MathHelpers.MersenneTwister();
		public static Brush RandomGraphBrush() {
			SolidColorBrush brush = new SolidColorBrush(RandomGraphColor());
			brush.Freeze();
			return brush;
		}

		public static Color RandomGraphColor() {
			double r, g, b, max, min, minV, maxV;
			max = GraphColorRandom.NextDouble() * 0.5 + 0.5;
			min = GraphColorRandom.NextDouble() * 0.5;
			r = GraphColorRandom.NextDouble();
			g = GraphColorRandom.NextDouble();
			b = GraphColorRandom.NextDouble();
			maxV = Math.Max(r, Math.Max(g, b));
			minV = Math.Min(r, Math.Min(g, b));
			r = (r - minV) / (maxV - minV) * (max - min) + min;
			g = (g - minV) / (maxV - minV) * (max - min) + min;
			b = (b - minV) / (maxV - minV) * (max - min) + min;
			if (r + g + b > 1.5) {
				double scale = 1.5 / (r + g + b);
				r *= scale; g *= scale; b *= scale;
			}
			return new Color {
				A = (byte)255,
				R = (byte)(255 * r + 0.5),
				G = (byte)(255 * g + 0.5),
				B = (byte)(255 * b + 0.5),
			};
		}

		struct ColorSimple
		{
			public double R, G, B;
			public double Sum { get { return R + G + B; } }
			public double SqrDistTo(ColorSimple other) { return sqr(R - other.R) + sqr(G - other.G) + sqr(B - other.B) - 0.1 * sqr(Sum - other.Sum); }
			static double sqr(double x) { return x * x; }
			public static ColorSimple Random(MersenneTwister rnd) {
				return new ColorSimple { R = rnd.NextDouble0To1(), G = rnd.NextDouble0To1(), B = rnd.NextDouble0To1() };
			}
			public void RepelFrom(ColorSimple other, double lr) {
				double sqrdist = Math.Max(SqrDistTo(other), lr);
				double force = lr / sqrdist;
				R = R + force * (R - other.R);
				G = G + force * (G - other.G);
				B = B + force * (B - other.B);
			}
			static double Clamp(double x) {
				return x < 0.0 ? 0.0 : x < 1.0 ? x : 1.0;
			}
			public Color ToWindowsColor() {
				return Color.FromRgb((byte)(255 * R + 0.5), (byte)(255 * G + 0.5), (byte)(255 * B + 0.5));
			}
			public void Min(ColorSimple other) {
				R = Math.Min(R, other.R);
				G = Math.Min(G, other.G);
				B = Math.Min(B, other.B);
			}
			public void Max(ColorSimple other) {
				R = Math.Max(R, other.R);
				G = Math.Max(G, other.G);
				B = Math.Max(B, other.B);
			}
			public void ScaleBack(ColorSimple min, ColorSimple max, MersenneTwister rnd)
			{
				if (max.R > min.R)
					R = 0.025 + 0.95 * (R - min.R) / (max.R - min.R);
				if (max.G > min.G)
					G = 0.025 + 0.95 * (G - min.G) / (max.G - min.G);
				if (max.B > min.B)
					B = 0.025 + 0.95 * (B - min.B) / (max.B - min.B);
				if (R == G && G == B)
					this = Random(rnd);
			}
		}

		public static Color[] MakeDistributedColors(int N) {
			MersenneTwister rnd = RndHelper.ThreadLocalRandom;
			int M = N;
			ColorSimple[] choices = Enumerable.Range(0, M).Select(i => ColorSimple.Random(rnd)).ToArray();
			ColorSimple black = new ColorSimple { R = 0, G = 0, B = 0 };
			ColorSimple white = new ColorSimple { R = 1, G = 1, B = 1 };

			for (int iter = 0; iter < 2000 + N; iter++) {
				double lr = 0.001 / Math.Sqrt(0.1 * iter + 1);
				ColorSimple min = new ColorSimple { R = double.MaxValue, G = double.MaxValue, B = double.MaxValue };
				ColorSimple max = new ColorSimple { R = double.MinValue, G = double.MinValue, B = double.MinValue };
				for (int i = 0; i < M; i++) {
#if DEBUG
					ColorSimple old = choices[i];
#endif
					choices[i].RepelFrom(white, lr);
					int other = rnd.Next(M - 1); //rand other in [0..M-1)
					if (other >= i) other++; //rand other in [0..M) with other != i
					if (N > 1)
						choices[i].RepelFrom(choices[other], lr);
					choices[i].RepelFrom(black, lr * 0.1);
					min.Min(choices[i]);
					max.Max(choices[i]);
				}
				for (int i = 0; i < M; i++)
					choices[i].ScaleBack(min, max,rnd);

				if (M > N) M--;
			}

			return choices.Select(c => c.ToWindowsColor()).ToArray();
		}


		public static Pen MakeDefaultPen(bool randomColor) {
			var newPen = new Pen(randomColor ? RandomGraphBrush() : Brushes.Black, 1.0);
			newPen.StartLineCap = PenLineCap.Round;
			newPen.EndLineCap = PenLineCap.Round;
			newPen.LineJoin = PenLineJoin.Round;
			return newPen;
		}
	}
}
