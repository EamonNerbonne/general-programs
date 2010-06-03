using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines
{
	public interface IVizPixelScatter : IVizEngine<Point[]>
	{
		double CoverageRatio { get; set; }
	}
	public static class VizPixelScatterHelpers
	{
		public const double SquareSidePerThickness = Math.PI / 4.0;
		public static double PointCountToThickness(int pointCount) { return 25.0 / (0.5 + Math.Log(Math.Max(pointCount, 1))); }

		public static void RecomputeBounds(Point[] points, double coverageX,double coverageY, out Rect outerBounds, out Rect coveredBounds) {
			if (HasPoints(points)) {
				outerBounds = ComputeOuterBounds(points);
				coveredBounds = ComputeInnerBoundsByRatio(points, coverageX,coverageY, outerBounds);
			} else {
				coveredBounds = outerBounds = Rect.Empty;
			}
		}
		#region RecomputeBounds Helpers
		static bool HasPoints(Point[] points) { return points != null && points.Length > 0; }

		static Rect ComputeOuterBounds(Point[] points) {
			Rect outerBounds = Rect.Empty;
			foreach (var point in points)
				outerBounds.Union(point);
			if(double.IsNaN(outerBounds.Width) || double.IsNaN(outerBounds.Height))
				throw new ArgumentException("Invalid point array!" + outerBounds);
			return outerBounds;
		}

		static Rect ComputeInnerBoundsByRatio(Point[] points, double coverageX, double coverageY, Rect completeBounds) {
			int cutoffEachSideX = (int)(0.5 * (1.0 - coverageX) * points.Length + 0.5);
			int cutoffEachSideY = (int)(0.5 * (1.0 - coverageY) * points.Length + 0.5);
			return
				cutoffEachSideX == 0 && cutoffEachSideY ==0? completeBounds :
				Math.Max(cutoffEachSideX,cutoffEachSideY) * 2 >= points.Length ? Rect.Empty :
				ComputeInnerBoundsByCutoff(points, cutoffEachSideX, cutoffEachSideY);
		}

		static Rect ComputeInnerBoundsByCutoff(Point[] points, int cutoffEachSideX, int cutoffEachSideY) {
			double[] xs = new double[points.Length];
			double[] ys = new double[points.Length];
			for (int i = 0; i < points.Length; i++) {
				xs[i] = points[i].X;
				ys[i] = points[i].Y;
			}
			Array.Sort(xs);
			Array.Sort(ys);
			int firstIndexX = cutoffEachSideX;
			int firstIndexY = cutoffEachSideY;
			int lastIndexX = points.Length - 1 - cutoffEachSideX;
			int lastIndexY = points.Length - 1 - cutoffEachSideY;
			Rect retval = new Rect(
					new Point(xs[firstIndexX], ys[firstIndexY]),
					new Point(xs[lastIndexX], ys[lastIndexY])
				);
			if (double.IsNaN(retval.Width) || double.IsNaN(retval.Height))
				throw new ArgumentException("Invalid point array!" + retval);

			return retval;
		}
		#endregion
	}
}
