using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines
{
	public interface IVizPixelScatter :IVizEngine<Point[]>
	{
		double CoverageRatio { get; set; }
		Color PointColor { get;set; }

	}
	public static class VizPixelScatterHelpers
	{
		public static void RecomputeBounds(Point[] points, double coverage, out Rect outerBounds, out Rect coveredBounds)
		{
			if (HasPoints(points))
			{
				outerBounds = ComputeOuterBounds(points);
				coveredBounds = ComputeInnerBoundsByRatio(points, coverage, outerBounds);
			}
			else
			{
				coveredBounds = outerBounds = Rect.Empty;
			}
		}

		#region RecomputeBounds Helpers
		static bool HasPoints(Point[] points) { return points != null && points.Length > 0; }

		static Rect ComputeOuterBounds(Point[] points)
		{
			Rect outerBounds = Rect.Empty;
			foreach (var point in points)
				outerBounds.Union(point);
			return outerBounds;
		}

		static Rect ComputeInnerBoundsByRatio(Point[] points, double coverageRatio, Rect completeBounds)
		{
			int cutoffEachSide = (int)(0.5 * (1.0 - coverageRatio) * points.Length + 0.5);
			return
				cutoffEachSide == 0 ? completeBounds :
				cutoffEachSide * 2 >= points.Length ? Rect.Empty :
				ComputeInnerBoundsByCutoff(points, cutoffEachSide);
		}

		static Rect ComputeInnerBoundsByCutoff(Point[] points, int cutoffEachSide)
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
			int firstIndex = cutoffEachSide;
			int lastIndex = points.Length - 1 - cutoffEachSide;
			return new Rect(
					new Point(xs[firstIndex], ys[firstIndex]),
					new Point(xs[lastIndex], ys[lastIndex])
				);
		}
		#endregion

	}
}
