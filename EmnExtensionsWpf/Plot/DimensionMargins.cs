using System;
using System.Windows;

namespace EmnExtensions.Wpf.Plot
{
	public struct DimensionMargins
	{
		public double AtStart { get; set; }
		public double AtEnd { get; set; }
		public double Sum { get { return AtStart + AtEnd; } }
		public static DimensionMargins FromThicknessX(Thickness thickness) { return new DimensionMargins { AtStart = thickness.Left, AtEnd = thickness.Right }; }
		public static DimensionMargins FromThicknessY(Thickness thickness) { return new DimensionMargins { AtStart = thickness.Top, AtEnd = thickness.Bottom }; }
		public static DimensionMargins Merge(DimensionMargins a, DimensionMargins b) { return new DimensionMargins { AtStart = Math.Max(a.AtStart, b.AtStart), AtEnd = Math.Max(a.AtEnd, b.AtEnd) }; }
	}
}
