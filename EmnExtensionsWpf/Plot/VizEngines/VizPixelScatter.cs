//#define SHAREMEM //useful on workstation GC
//#define TRACKUSAGE //handy to see how fast GUI is.

using System;
using System.Windows;
using EmnExtensions.Algorithms;

namespace EmnExtensions.Wpf.Plot.VizEngines
{
    public interface IVizPixelScatter : IVizEngine<Point[]>
    {
        double CoverageRatio { get; set; }
        double CoverageGradient { get; set; }
        int? OverridePointCountEstimate { get; set; }
    }

    public static class VizPixelScatterHelpers
    {
        public const double SquareSidePerThickness = Math.PI / 4.0;

        //public static double PointCountToThickness(int pointCount) { return 15.0 / (0.5 + Math.Log(Math.Max(pointCount, 1))); }
        public static double PointCountToThickness(int pointCount)
            => 225.0 / Math.Sqrt(pointCount + 2500);

        public static void RecomputeBounds(Point[] points, double coverageX, double coverageY, double coverageGrad, out Rect outerBounds, out Rect coveredBounds)
        {
            if (HasPoints(points)) {
                outerBounds = ComputeOuterBounds(points);
                coveredBounds = ComputeInnerBoundsByRatio(points, coverageX, coverageY, coverageGrad, outerBounds);
            } else {
                coveredBounds = outerBounds = Rect.Empty;
            }
        }

        #region RecomputeBounds Helpers
        static bool HasPoints(Point[] points)
            => points != null && points.Length > 0;

        // ReSharper disable ParameterTypeCanBeEnumerable.Global
        public static Rect ComputeOuterBounds(Point[] points)
        {
            // ReSharper restore ParameterTypeCanBeEnumerable.Global
            double xmin = double.MaxValue, ymin = double.MaxValue, ymax = double.MinValue, xmax = double.MinValue;
            foreach (var point in points) {
                xmin = Math.Min(xmin, point.X);
                ymin = Math.Min(ymin, point.Y);
                xmax = Math.Max(xmax, point.X);
                ymax = Math.Max(ymax, point.Y);
            }

            var outerBounds = new Rect(xmin, ymin, xmax - xmin, ymax - ymin);
            if (double.IsNaN(outerBounds.Width) || double.IsNaN(outerBounds.Height)) {
                throw new PlotVizException("Invalid point array!" + outerBounds);
            }

            return outerBounds;
        }

        public static Rect ComputeInnerBoundsByRatio(Point[] points, double coverageX, double coverageY, double coverageGrad, Rect completeBounds)
        {
            var cutoffEachSideX = (int)(0.5 * (1.0 - coverageX) * points.Length + 0.5);
            var cutoffEachSideY = (int)(0.5 * (1.0 - coverageY) * points.Length + 0.5);
            return
                cutoffEachSideX == 0 && cutoffEachSideY == 0 ? completeBounds : ComputeInnerBoundsByCutoff(points, cutoffEachSideX, cutoffEachSideY, coverageGrad);
        }

#if TRACKUSAGE
        static int hitcount;
        static readonly DateTime start = DateTime.Now;
        static VizPixelScatterHelpers() {
            AppDomain.CurrentDomain.ProcessExit += (o, e) =>
                System.IO.File.AppendAllText(@"D:\emnplot.log", "HitCount: " + hitcount + ", time:" + (DateTime.Now - start) + "\n");
        }
#endif
#if SHAREMEM
        static object sync = new object();
        static double[] vals = new double[0];
#endif
        static Rect ComputeInnerBoundsByCutoff(Point[] points, int cutoffEachSideX, int cutoffEachSideY, double coverageGrad)
        {
#if SHAREMEM
            lock (sync)
#endif
            {
#if TRACKUSAGE
                hitcount++;
#endif
#if SHAREMEM
                if (vals.Length < points.Length)
#else
                // ReSharper disable JoinDeclarationAndInitializer
                double[] vals;
                // ReSharper restore JoinDeclarationAndInitializer
#endif
                vals = new double[points.Length];
                for (var i = 0; i < points.Length; i++) {
                    vals[i] = points[i].X;
                }

                var xBounds = TrimWithMinimumGradient(vals, points.Length, cutoffEachSideX, coverageGrad);
                for (var i = 0; i < points.Length; i++) {
                    vals[i] = points[i].Y;
                }

                var yBounds = TrimWithMinimumGradient(vals, points.Length, cutoffEachSideY, coverageGrad);

                if (xBounds.IsEmpty || yBounds.IsEmpty) {
                    return Rect.Empty;
                }

                if (!xBounds.Length.IsFinite() || !yBounds.Length.IsFinite()) {
                    throw new PlotVizException("Invalid point array!");
                }

                return new(xBounds.Start, yBounds.Start, xBounds.Length, yBounds.Length);
            }
        }

        static DimensionBounds TrimWithMinimumGradient(double[] data, int datalen, int maxCutoff, double requiredGradient)
        {
            if (datalen == 0) {
                return DimensionBounds.Empty;
            }

            if (maxCutoff < datalen * 0.3) {
                SelectionAlgorithm.QuickSelect(data, datalen - 1 - maxCutoff, 0, datalen);
                SelectionAlgorithm.QuickSelect(data, maxCutoff, 0, datalen);
                Array.Sort(data, 0, maxCutoff);
                Array.Sort(data, datalen - maxCutoff, maxCutoff);
            } else {
                Array.Sort(data, 0, datalen);
            }

            var xLen = data[datalen - 1] - data[0];
            if (xLen == 0.0) {
                return new() { Start = data[0], End = data[datalen - 1] };
            }

            var startCutoff = maxCutoff;
            var endCutoff = maxCutoff;
            while (startCutoff != 0 && (startCutoff >= datalen || requiredGradient * startCutoff / datalen > (data[startCutoff] - data[0]) / xLen)) {
                startCutoff--;
            }

            while (endCutoff != 0 && (endCutoff >= datalen - startCutoff || requiredGradient * endCutoff / datalen > (data[datalen - 1] - data[datalen - 1 - endCutoff]) / xLen)) {
                endCutoff--;
            }

            return startCutoff < datalen - 1 - endCutoff
                ? new() { Start = data[startCutoff], End = data[datalen - 1 - endCutoff] }
                : DimensionBounds.Empty;
        }
        #endregion
    }
}
