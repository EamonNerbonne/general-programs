using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace EmnExtensions.Wpf.Plot
{
	public class GraphablePixelScatterPlot : GraphableData
	{
		public GraphablePixelScatterPlot() { Margin = new Thickness(0.5); BitmapScalingMode = BitmapScalingMode.Linear; }
		double m_dpiX = 96.0, m_dpiY = 96.0;
		bool m_useDiamondPoints;
		public bool UseDiamondPoints { get { return m_useDiamondPoints; } set { m_useDiamondPoints = value; OnChange(GraphChangeEffects.GraphProjection); } }
		public double DpiX { get { return m_dpiX; } set { if (value != m_dpiX) { m_dpiX = value; OnChange(GraphChangeEffects.GraphProjection); } } }
		public double DpiY { get { return m_dpiY; } set { if (value != m_dpiY) { m_dpiY = value; OnChange(GraphChangeEffects.GraphProjection); } } }
		public BitmapScalingMode BitmapScalingMode { get { return RenderOptions.GetBitmapScalingMode(painting); } set { RenderOptions.SetBitmapScalingMode(painting, value); } }

		DrawingGroup painting = new DrawingGroup();
		WriteableBitmap bmp;
		Point bmpOffset;
		const int extraPix = 512;
		Point[] m_points;
		Rect m_outerBounds = Rect.Empty;
		public Point[] Points { get { return m_points; } set { if (value != m_points) { m_points = value; DataBounds = ComputeBounds(); } } }
		double m_coverage = 1.0;
		public double CoverageRatio { get { return m_coverage; } set { if (value != m_coverage) { m_coverage = value; DataBounds = ComputeBounds(); } } }

		private Rect ComputeBounds() {
			if (m_points == null || m_points.Length == 0)
				return Rect.Empty;
			else {
				int cutoff = (int)(0.5 + 0.5 * (1.0 - m_coverage) * m_points.Length);
				m_outerBounds = Rect.Empty;
				foreach (var point in m_points)
					m_outerBounds.Union(point);
				if (cutoff == 0) {
					return m_outerBounds;
				} else if (cutoff * 2 >= m_points.Length) {
					return Rect.Empty;
				} else {
					double[] xs = new double[m_points.Length];
					double[] ys = new double[m_points.Length];
					for (int i = 0; i < m_points.Length; i++) {
						xs[i] = m_points[i].X;
						ys[i] = m_points[i].Y;
					}
					Array.Sort(xs);
					Array.Sort(ys);
					return new Rect(new Point(xs[cutoff], ys[cutoff]), new Point(xs[m_points.Length - 1 - cutoff], ys[m_points.Length - 1 - cutoff]));
				}
			}
		}

		Color m_pointColor;
		public Color PointColor { get { return m_pointColor; } set { if (value != m_pointColor) { m_pointColor = value; OnChange(GraphChangeEffects.GraphProjection); } } }

		public override void DrawGraph(System.Windows.Media.DrawingContext context) {
			context.PushGuidelineSet(new GuidelineSet(new[] { 0.0 }, new[] { 0.0 }));
			context.DrawDrawing(painting);
			context.Pop();
			Console.WriteLine("redraw");
		}

		static Rect SnapRect(Rect r, double multX, double multY) { return new Rect(new Point(Math.Floor(r.Left / multX) * multX, Math.Floor(r.Top / multY) * multY), new Point(Math.Ceiling((r.Right + 0.01) / multX) * multX, Math.Ceiling((r.Bottom + 0.01) / multY) * multY)); }

		public override void SetTransform(Matrix matrix, Rect displayClip) {
			if (matrix.IsIdentity) {
				using (var ctx = painting.Open()) return;
			}

			Rect outerDispBounds = Rect.Transform(m_outerBounds, matrix);
			outerDispBounds.Intersect(displayClip);
			outerDispBounds = SnapRect(outerDispBounds, 96.0 / m_dpiX, 96.0 / m_dpiX);

			int pW = (int)Math.Ceiling(outerDispBounds.Width * m_dpiX / 96.0);
			int pH = (int)Math.Ceiling(outerDispBounds.Height * m_dpiY / 96.0);
			uint[] image = new uint[pW * pH];
			image.Count();
			foreach (var point in Points) {
				var displaypoint = matrix.Transform(point);
				int x = (int)((displaypoint.X - outerDispBounds.X) * m_dpiX / 96.0);
				int y = (int)((displaypoint.Y - outerDispBounds.Y) * m_dpiY / 96.0);
				if (x >= 0 && x < pW && y >= 0 && y < pH)
				{
					image[x + pW * y]++;
					if (UseDiamondPoints)
					{
						if (x - 1 >= 0)
							image[x -1 + pW * y]++;
						if (x +1 <pW)
							image[x + 1 + pW * y]++;
						if (y-1>=0)
							image[x + pW * (y-1)]++;
						if (y + 1  < pH)
							image[x + pW * (y + 1)]++;
					}
				}
			} // so now we've counted the number of pixels in each position...

			uint maxOverlap = image.Max();
			uint[] alphaLookup = new uint[maxOverlap + 1];
			for (int i = 0; i < alphaLookup.Length; i++)
				alphaLookup[i] = ((uint)((1.0 - Math.Pow(1.0 - PointColor.ScA, i)) * 255.5) << 24);

			uint nativeColor = PointColor.ToNativeColor() & 0x00ffffff;
			double transparency = 1.0 - PointColor.ScA;
			for (int pxI = 0; pxI < image.Length; pxI++)
				image[pxI] = nativeColor | alphaLookup[image[pxI]]; // ((uint)((1.0 - Math.Pow(transparency, image[pxI])) * 255.5) << 24);

			if (bmp == null || bmp.PixelWidth < pW || bmp.PixelWidth < pH)
			{
				bmp = new WriteableBitmap(pW + extraPix, pH + extraPix, m_dpiX, m_dpiY, PixelFormats.Bgra32, null);
				bmpOffset.X = outerDispBounds.X+1; //set not-equal.
				Console.WriteLine("new WriteableBitmap");
			}

			try
			{
				bmp.Lock();
				bmp.WritePixels(new Int32Rect(0, 0, pW, pH), image, pW * sizeof(uint), 0);
			}
			finally
			{
				bmp.Unlock();
			}

			if (bmpOffset.X != outerDispBounds.X || bmpOffset.Y != outerDispBounds.Y)
				using (var ctx = painting.Open()) {
					ctx.DrawImage(bmp, new Rect(outerDispBounds.X, outerDispBounds.Y, bmp.Width, bmp.Height));
					Console.WriteLine("repaint: X: {0}->{1}, Y: {2}->{3}", bmpOffset.X, outerDispBounds.X, bmpOffset.Y,outerDispBounds.Y);
					bmpOffset = outerDispBounds.TopLeft;
				}

			painting.ClipGeometry = new RectangleGeometry(displayClip);
			//painting.

			Console.WriteLine("retransform");
		}
	}
}
