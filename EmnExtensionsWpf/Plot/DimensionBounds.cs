using System;
using System.Windows;

namespace EmnExtensions.Wpf.Plot
{
	public struct DimensionBounds
	{
		public double Min { get; set; }
		public double Max { get; set; }
		public double Length { get { return Max - Min; } }
		public bool EncompassesValue(double value) { return value >= Min && value <= Max; }

		public void Translate(double offset) { Min += offset; Max += offset; }
		public void Scale(double factor) { Min *= factor; Max *= factor; }

		public static DimensionBounds Undefined { get { return new DimensionBounds { Min = double.NaN, Max = double.NaN }; } }
		public static DimensionBounds FromRectX(Rect r) { return new DimensionBounds { Min = r.X, Max = r.Right }; }
		public static DimensionBounds FromRectY(Rect r) { return new DimensionBounds { Min = r.Y, Max = r.Bottom }; }
		public static DimensionBounds Merge(DimensionBounds a, DimensionBounds b) { return new DimensionBounds { Min = Math.Min(a.Min, b.Min), Max = Math.Max(a.Max, b.Max) }; }
		public static double MergeQuality(DimensionBounds a, DimensionBounds b) { double mergedLength = Merge(a, b).Length; return a.Length * b.Length / mergedLength / mergedLength; }
	}
}
