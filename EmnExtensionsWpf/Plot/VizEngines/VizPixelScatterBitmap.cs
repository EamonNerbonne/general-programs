using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;

namespace EmnExtensions.Wpf.Plot.VizEngines
{
	public class VizPixelScatterBitmap : VizDynamicBitmap<Point[]>, IVizPixelScatter
	//for efficiency reasons, we accept data in a Point[] rather than the more general IEnumerable<Point>
	{
		bool m_useDiamondPoints = true;
		Rect m_OuterDataBounds = Rect.Empty;
		double m_CoverageRatio = 0.9999;
		Color m_pointColor;
		uint[] m_image;
		Point[] currentPoints;

		public bool UseDiamondPoints { get { return m_useDiamondPoints; } set { m_useDiamondPoints = value; TriggerChange(GraphChange.Projection); } }
		protected override Rect? OuterDataBound { get { return m_OuterDataBounds; } }
		public double CoverageRatio { get { return m_CoverageRatio; } set { if (value != m_CoverageRatio) { m_CoverageRatio = value; RecomputeBounds(currentPoints); } } }
		public Color PointColor { get { return m_pointColor; } set { if (value != m_pointColor) { m_pointColor = value; TriggerChange(GraphChange.Projection); } } }

		protected override void UpdateBitmap(Point[] data, int pW, int pH, Matrix dataToBitmap)
		{
			Trace.WriteLine("UpdateBitmap");

			if (dataToBitmap.IsIdentity) return;//this is the default mapping; it may occur when generating a scatter plot without data - don't bother plotting.

			Make2dHistogramInRegion(pW, pH, dataToBitmap);
			ConvertHistogramToColorDensityImage(pW, pH);
			CopyImageRegionToWriteableBitmap(pW, pH);
		}

		#region UpdateBitmap Helpers
		void Make2dHistogramInRegion(int pW, int pH, Matrix dataToBitmap)
		{
			MakeVisibleRegionEmpty(pW, pH);

			if (UseDiamondPoints)
				MakeDiamondPoint2dHistogram(pW, pH, dataToBitmap);
			else
				MakeSinglePoint2dHistogram(pW, pH, dataToBitmap);
		}

		void MakeVisibleRegionEmpty(int pW, int pH)
		{
			if (m_image == null || m_image.Length < pW * pH)
				m_image = new uint[pW * pH];
			else
				for (int i = 0; i < pW * pH; i++)
					m_image[i] = 0;
		}

		void MakeDiamondPoint2dHistogram(int pW, int pH, Matrix dataToBitmap)
		{
			foreach (var point in currentPoints)
			{
				Point displaypoint = dataToBitmap.Transform(point);
				int x = (int)(displaypoint.X);
				int y = (int)(displaypoint.Y);
				if (x >= 1 && x < pW - 1 && y >= 1 && y < pH - 1)
				{
					m_image[x + pW * y]++;
					m_image[x - 1 + pW * y]++;
					m_image[x + 1 + pW * y]++;
					m_image[x + pW * (y - 1)]++;
					m_image[x + pW * (y + 1)]++;
				}
			}
		}

		void MakeSinglePoint2dHistogram(int pW, int pH, Matrix dataToBitmap)
		{
			foreach (var point in currentPoints)
			{
				Point displaypoint = dataToBitmap.Transform(point);
				int x = (int)(displaypoint.X);
				int y = (int)(displaypoint.Y);
				if (x >= 0 && x < pW && y >= 0 && y < pH)
					m_image[x + pW * y]++;
			}
		}

		void ConvertHistogramToColorDensityImage(int pW, int pH)
		{
			int numPixels = pW * pH;
			uint[] alphaLookup = PregenerateAlphaLookup(PointColor.ScA, m_image, numPixels);
			uint nativeColorWithoutAlpha = PointColor.ToNativeColor() & 0x00ffffff;

			for (int pxI = 0; pxI < numPixels; pxI++)
				m_image[pxI] = nativeColorWithoutAlpha | alphaLookup[m_image[pxI]];
		}

		void CopyImageRegionToWriteableBitmap(int pW, int pH)
		{
			m_bmp.WritePixels(
				sourceRect: new Int32Rect(0, 0, pW, pH),
				sourceBuffer: m_image,
				sourceBufferStride: pW * sizeof(uint),
				destinationX: 0,
				destinationY: 0);
		}

		static uint[] PregenerateAlphaLookup(double alpha, uint[] image, int numPixels)
		{
			uint maximalOverlapCount = ValueOfMax(image, 0, numPixels);
			return MakeAlphaLookupUpto(alpha, maximalOverlapCount);
		}

		static uint[] MakeAlphaLookupUpto(double alpha, uint maxOverlap)
		{
			double transparencyPerOverlap = 1.0 - alpha;
			uint[] alphaLookup = new uint[maxOverlap + 1];
			for (int overlap = 0; overlap < alphaLookup.Length; overlap++)
			{
				double overlappingAlpha = 1.0 - Math.Pow(transparencyPerOverlap, overlap);
				alphaLookup[overlap] = (uint)(overlappingAlpha * 255.5) << 24;
			}
			return alphaLookup;
		}

		static uint ValueOfMax(uint[] m_image, int start, int end)
		{
			uint maxCount = 0;
			for (int i = start; i < end; i++)
				if (m_image[i] > maxCount)
					maxCount = m_image[i];
			return maxCount;
		}
		#endregion

		public override void DataChanged(Point[] newData)
		{
			currentPoints = newData;
			RecomputeBounds(newData);
			TriggerChange(GraphChange.Projection); //because we need to relayout the points in the plot
		}

		void RecomputeBounds(Point[] points)
		{
			Rect innerBounds;
			VizPixelScatterHelpers.RecomputeBounds(points, CoverageRatio, out m_OuterDataBounds, out innerBounds);
			SetDataBounds(innerBounds);
		}
	}
}
