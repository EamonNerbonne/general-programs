using System;
using System.Windows;

namespace EmnExtensions.Wpf.Plot
{
	public struct DimensionBounds
	{
		public double Start { get; set; }
		public double End { get; set; }
		public double Length { get { return End - Start; } }
		public static DimensionBounds Undefined { get { return new DimensionBounds { Start = double.NaN, End = double.NaN }; } }
		public static DimensionBounds FromRectX(Rect r) { return new DimensionBounds { Start = r.X, End = r.Right }; }
		public static DimensionBounds FromRectY(Rect r) { return new DimensionBounds { Start = r.Y, End = r.Bottom }; }
		public static DimensionBounds Merge(DimensionBounds a, DimensionBounds b) { return new DimensionBounds { Start = Math.Min(a.Start, b.Start), End = Math.Max(a.End, b.End) }; }
		public static double MergeQuality(DimensionBounds a, DimensionBounds b) { double mergedLength = Merge(a, b).Length; return a.Length * b.Length / mergedLength / mergedLength; }
	}
}
