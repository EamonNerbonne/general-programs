using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;

namespace EmnExtensions.Wpf.Plot
{
	public class VizPixelScatterBitmap : VizDynamicBitmap
	{
		public VizPixelScatterBitmap(PlotDataBase owner) : base(owner) { }

		bool m_useDiamondPoints;
		public bool UseDiamondPoints { get { return m_useDiamondPoints; } set { m_useDiamondPoints = value; OnChange(GraphChange.Projection); } }

		Point[] Points { get { return (Point[])m_owner.RawData; } }

		Rect m_outerBounds = Rect.Empty;
		protected override Rect? OuterDataBound { get { return m_outerBounds; } }

		double m_coverage = 1.0;
		public double CoverageRatio { get { return m_coverage; } set { if (value != m_coverage) { m_coverage = value; DataBounds = ComputeBounds(); } } }
		private Rect ComputeBounds()
		{
			Point[] points = Points;
			if (points == null || points.Length == 0)
				return Rect.Empty;
			else
			{
				int cutoff = (int)(0.5 + 0.5 * (1.0 - m_coverage) * points.Length);
				m_outerBounds = Rect.Empty;
				foreach (var point in points)
					m_outerBounds.Union(point);
				if (cutoff == 0)
				{
					return m_outerBounds;
				}
				else if (cutoff * 2 >= points.Length)
				{
					return Rect.Empty;
				}
				else
				{
					double[] xs = new double[points.Length];
					double[] ys = new double[points.Length];
					for (int i = 0; i < points.Length; i++)
					{
						xs[i] = points[i].X;
						ys[i] = points[i].Y;
					}
					Array.Sort(xs);
					Array.Sort(ys);
					return new Rect(new Point(xs[cutoff], ys[cutoff]), new Point(xs[points.Length - 1 - cutoff], ys[points.Length - 1 - cutoff]));
				}
			}
		}


		Color m_pointColor;
		public Color PointColor { get { return m_pointColor; } set { if (value != m_pointColor) { m_pointColor = value; OnChange(GraphChange.Projection); } } }

		uint[] image;

		protected override void UpdateBitmap(int pW, int pH, Matrix dataToBitmap)
		{
			if (dataToBitmap.IsIdentity)
				return;//TODO: clear bitmap??

			if (image == null || image.Length < pW * pH)
				image = new uint[pW * pH];
			else
				for (int i = 0; i < pW * pH; i++)
					image[i] = 0;

			if (UseDiamondPoints) //for performance, if-lifted out of loop.
				foreach (var point in Points)
				{
					var displaypoint = dataToBitmap.Transform(point);

					int x = (int)(displaypoint.X);
					int y = (int)(displaypoint.Y);
					if (x >= 1 && x < pW - 1 && y >= 1 && y < pH - 1)
					{
						image[x + pW * y]++;
						image[x - 1 + pW * y]++;
						image[x + 1 + pW * y]++;
						image[x + pW * (y - 1)]++;
						image[x + pW * (y + 1)]++;
					}
				}
			else //non-diamond case 
				foreach (var point in Points)
				{
					var displaypoint = dataToBitmap.Transform(point);
					int x = (int)(displaypoint.X);
					int y = (int)(displaypoint.Y);
					if (x >= 0 && x < pW && y >= 0 && y < pH)
						image[x + pW * y]++;
				} // so now we've counted the number of pixels in each position...

			uint maxOverlap = 0;
			for (int i = 0; i < pW * pH; i++)
				if (image[i] > maxOverlap)
					maxOverlap = image[i];
			uint[] alphaLookup = new uint[maxOverlap + 1];
			for (int i = 0; i < alphaLookup.Length; i++)
				alphaLookup[i] = ((uint)((1.0 - Math.Pow(1.0 - PointColor.ScA, i)) * 255.5) << 24);

			uint nativeColor = PointColor.ToNativeColor() & 0x00ffffff;
			double transparency = 1.0 - PointColor.ScA;
			for (int pxI = 0; pxI < pW * pH; pxI++)
				image[pxI] = nativeColor | alphaLookup[image[pxI]]; // ((uint)((1.0 - Math.Pow(transparency, image[pxI])) * 255.5) << 24);

			m_bmp.WritePixels(new Int32Rect(0, 0, pW, pH), image, pW * sizeof(uint), 0, 0);

			//painting.
			Trace.WriteLine("retransform");
		}

		public override void DataChanged(object newData)
		{
			DataBounds = ComputeBounds();
			OnChange(GraphChange.Projection);
		}
	}
}
