﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines {
	public interface IVizPixelScatter : IVizEngine<Point[]> {
		double CoverageRatio { get; set; }
	}
	public static class VizPixelScatterHelpers {
		public const double SquareSidePerThickness = Math.PI / 4.0;
		public static double PointCountToThickness(int pointCount) { return 15.0 / (0.5 + Math.Log(Math.Max(pointCount, 1))); }

		public static void RecomputeBounds(Point[] points, double coverageX, double coverageY, double coverageGrad, out Rect outerBounds, out Rect coveredBounds) {
			if (HasPoints(points)) {
				outerBounds = ComputeOuterBounds(points);
				coveredBounds = ComputeInnerBoundsByRatio(points, coverageX, coverageY, coverageGrad, outerBounds);
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
			if (double.IsNaN(outerBounds.Width) || double.IsNaN(outerBounds.Height))
				throw new ArgumentException("Invalid point array!" + outerBounds);
			return outerBounds;
		}

		static Rect ComputeInnerBoundsByRatio(Point[] points, double coverageX, double coverageY, double coverageGrad, Rect completeBounds) {
			int cutoffEachSideX = (int)(0.5 * (1.0 - coverageX) * points.Length + 0.5);
			int cutoffEachSideY = (int)(0.5 * (1.0 - coverageY) * points.Length + 0.5);
			return
				cutoffEachSideX == 0 && cutoffEachSideY == 0 ? completeBounds :
				ComputeInnerBoundsByCutoff(points, cutoffEachSideX, cutoffEachSideY, coverageGrad);
		}

		static Rect ComputeInnerBoundsByCutoff(Point[] points, int cutoffEachSideX, int cutoffEachSideY, double coverageGrad) {
			double[] xs = new double[points.Length];
			double[] ys = new double[points.Length];
			for (int i = 0; i < points.Length; i++) {
				xs[i] = points[i].X;
				ys[i] = points[i].Y;
			}
			DimensionBounds
				xBounds = TrimWithMinimumGradient(xs, cutoffEachSideX, coverageGrad),
				yBounds = TrimWithMinimumGradient(ys, cutoffEachSideY, coverageGrad);

			if (xBounds.IsEmpty || yBounds.IsEmpty) return Rect.Empty;
			else if (!xBounds.Length.IsFinite() || !yBounds.Length.IsFinite()) throw new ArgumentException("Invalid point array!");
			else return new Rect(xBounds.Start, yBounds.Start, xBounds.Length, yBounds.Length);
		}

		static DimensionBounds TrimWithMinimumGradient(double[] data, int maxCutoff, double requiredGradient) {
			if (data.Length == 0) return DimensionBounds.Empty;
			Array.Sort(data);
			double xLen = data[data.Length - 1] - data[0];
			if (xLen == 0.0) return new DimensionBounds { Start = data[0], End = data[data.Length - 1] };
			int startCutoff = maxCutoff;
			int endCutoff = maxCutoff;
			while (startCutoff != 0 && (startCutoff >= data.Length || requiredGradient * startCutoff / (double)data.Length > (data[startCutoff] - data[0]) / xLen))
				startCutoff--;
			while (endCutoff != 0 && (endCutoff >= data.Length - startCutoff || requiredGradient * endCutoff / (double)data.Length > (data[data.Length - 1] - data[data.Length - 1 - endCutoff]) / xLen))
				endCutoff--;
			return startCutoff < data.Length - 1 - endCutoff
			? new DimensionBounds { Start = data[startCutoff], End = data[data.Length - 1 - endCutoff] }
			: DimensionBounds.Empty;
		}
		#endregion
	}
}
