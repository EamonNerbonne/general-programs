using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace EmnExtensions.Wpf
{
	public static class GraphRandomPen
	{
		static Random GraphColorRandom = new EmnExtensions.MathHelpers.MersenneTwister();
		static Brush RandomGraphColor() {
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
			SolidColorBrush brush = new SolidColorBrush(
				new Color {
					A = (byte)255,
					R = (byte)(255 * r + 0.5),
					G = (byte)(255 * g + 0.5),
					B = (byte)(255 * b + 0.5),
				}
				);
			brush.Freeze();
			return brush;
		}
		public static Pen MakeDefaultPen(bool randomColor) {
			var newPen = new Pen(randomColor ? RandomGraphColor() : Brushes.Black, 1.0);
			newPen.StartLineCap = PenLineCap.Round;
			newPen.EndLineCap = PenLineCap.Round;
			newPen.LineJoin = PenLineJoin.Round;
			return newPen;
		}
	}
}
