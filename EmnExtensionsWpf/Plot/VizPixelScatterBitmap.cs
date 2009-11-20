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
		public double CoverageRatio { get { return m_coverage; } set { if (value != m_coverage) { m_coverage = value; RecomputeBounds(); } } }
		private void RecomputeBounds() {
			if (!HasPoints())
				DataBounds = m_outerBounds = Rect.Empty;
			else {
				m_outerBounds = ComputeOuterBounds(Points);
				DataBounds = ComputeInnerBoundsByRatio(Points, m_coverage, m_outerBounds);
			}
		}

		private bool HasPoints() { return Points != null && Points.Length > 0; }

		private static Rect ComputeOuterBounds(Point[] points) {
			Rect outerBounds = Rect.Empty;
			foreach (var point in points)
				outerBounds.Union(point);
			return outerBounds;
		}

		private static Rect ComputeInnerBoundsByRatio(Point[] points, double coverageRatio, Rect completeBounds) {
			int cutoffEachSide = (int)(0.5 * (1.0 - coverageRatio) * points.Length + 0.5);
			return
				cutoffEachSide == 0 ? completeBounds :
				cutoffEachSide * 2 >= points.Length ? Rect.Empty :
				ComputeInnerBoundsByCutoff(points, cutoffEachSide);
		}

		private static Rect ComputeInnerBoundsByCutoff(Point[] points, int cutoffEachSide) {
			double[] xs = new double[points.Length];
			double[] ys = new double[points.Length];
			for (int i = 0; i < points.Length; i++) {
				xs[i] = points[i].X;
				ys[i] = points[i].Y;
			}
			Array.Sort(xs);
			Array.Sort(ys);
			int firstIndex = cutoffEachSide;
			int lastIndex = points.Length - 1 - cutoffEachSide;
			return new Rect(
					new Point(xs[firstIndex], ys[firstIndex]),
					new Point(xs[lastIndex], ys[lastIndex])
				);
		}

		Color m_pointColor;
		public Color PointColor { get { return m_pointColor; } set { if (value != m_pointColor) { m_pointColor = value; OnChange(GraphChange.Projection); } } }

		uint[] m_image;


		protected override void UpdateBitmap(int pW, int pH, Matrix dataToBitmap) {
			if (dataToBitmap.IsIdentity) return;//TODO: should I clear the bitmap when no meaningful transform??
			Trace.WriteLine("UpdateBitmap");
			ClearImageSquare(pW, pH);
			CreateHistogramImage(pW, pH, dataToBitmap);
			ConvertHistogramToColorDensityImage(pW, pH);
			m_bmp.WritePixels(new Int32Rect(0, 0, pW, pH), m_image, pW * sizeof(uint), 0, 0);
		}

		private void CreateHistogramImage(int pW, int pH, Matrix dataToBitmap) {
			if (UseDiamondPoints)
				CreateDiamondPointHistogram(pW, pH, dataToBitmap);
			else
				CreateSinglePointHistogram(pW, pH, dataToBitmap);
		}

		private void CreateDiamondPointHistogram(int pW, int pH, Matrix dataToBitmap) {
			foreach (var point in Points) {
				var displaypoint = dataToBitmap.Transform(point);
				int x = (int)(displaypoint.X);
				int y = (int)(displaypoint.Y);
				if (x >= 1 && x < pW - 1 && y >= 1 && y < pH - 1) {
					m_image[x + pW * y]++;
					m_image[x - 1 + pW * y]++;
					m_image[x + 1 + pW * y]++;
					m_image[x + pW * (y - 1)]++;
					m_image[x + pW * (y + 1)]++;
				}
			}
		}

		private void CreateSinglePointHistogram(int pW, int pH, Matrix dataToBitmap) {
			foreach (var point in Points) {
				var displaypoint = dataToBitmap.Transform(point);
				int x = (int)(displaypoint.X);
				int y = (int)(displaypoint.Y);
				if (x >= 0 && x < pW && y >= 0 && y < pH)
					m_image[x + pW * y]++;
			}
		}

		private void ConvertHistogramToColorDensityImage(int pW, int pH) {
			int numPixels = pW * pH;
			uint maxCount = MaxCount(m_image, 0, numPixels);
			uint[] alphaLookup = MakeAlphaLookup(maxCount, PointColor.ScA);
			uint nativeColorWithoutAlpha = PointColor.ToNativeColor() & 0x00ffffff;
			for (int pxI = 0; pxI < numPixels; pxI++)
				m_image[pxI] = nativeColorWithoutAlpha | alphaLookup[m_image[pxI]];
		}

		private static uint[] MakeAlphaLookup(uint maxOverlap, double alpha) {
			double transparency = 1.0 - alpha;
			uint[] alphaLookup = new uint[maxOverlap + 1];
			for (int i = 0; i < alphaLookup.Length; i++)
				alphaLookup[i] = ((uint)((1.0 - Math.Pow(transparency, i)) * 255.5) << 24);
			return alphaLookup;
		}

		private void ClearImageSquare(int pW, int pH) {
			if (m_image == null || m_image.Length < pW * pH)
				m_image = new uint[pW * pH];
			else
				for (int i = 0; i < pW * pH; i++)
					m_image[i] = 0;
		}

		private static uint MaxCount(uint[] m_image, int start, int end) {
			uint maxCount = 0;
			for (int i = start; i < end; i++)
				if (m_image[i] > maxCount)
					maxCount = m_image[i];
			return maxCount;
		}

		public override void DataChanged(object newData) {
			RecomputeBounds();
			OnChange(GraphChange.Projection); //because we need to relayout the points in the plot
		}
	}
}
