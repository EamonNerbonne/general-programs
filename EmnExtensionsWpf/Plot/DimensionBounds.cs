using System;
using System.Windows;
using System.ComponentModel;
using EmnExtensions.Text;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics;


namespace EmnExtensions.Wpf.Plot
{
	[DebuggerDisplay("[{Start}, {End}]")]
	[TypeConverter(typeof(DimensionBoundsConverter))]
	public struct DimensionBounds
	{
		public double Start { get; set; }
		public double End { get; set; }
		public bool FlippedOrder { get { return End < Start; } }
		public double Min { get { return Math.Min(Start, End); } }
		public double Max { get { return Math.Max(Start, End); } }
		public bool IsEmpty { get { return double.IsPositiveInfinity(Start) && double.IsPositiveInfinity(End); } }
		public double Length { get { return Math.Abs(End - Start); } }

		public bool EncompassesValue(double value) { return value >= Start && value <= End; }
		public DimensionBounds UnionWith(params double[] vals) { return UnionWith(vals.AsEnumerable()); }
		public DimensionBounds UnionWith(IEnumerable<double> vals)
		{
			double min = this.Min, max = this.Max;
			if (IsEmpty)
				max = double.NegativeInfinity;
			foreach (double val in vals)
			{
				if (val < min)
					min = val;
				if (val > max)
					max = val;
			}

			if (double.IsNegativeInfinity(max))
				return DimensionBounds.Empty;
			else
				return FlippedOrder ? new DimensionBounds { Start = max, End = min } : new DimensionBounds { Start = min, End = max };
		}

		public void Translate(double offset) { Start += offset; End += offset; }
		public void Scale(double factor) { Start *= factor; End *= factor; }

		public static DimensionBounds Empty { get { return new DimensionBounds { Start = double.PositiveInfinity, End = double.PositiveInfinity }; } }
		public static DimensionBounds FromRectX(Rect r) { return r.IsEmpty ? Empty : new DimensionBounds { Start = r.X, End = r.Right }; }
		public static DimensionBounds FromRectY(Rect r) { return r.IsEmpty ? Empty : new DimensionBounds { Start = r.Y, End = r.Bottom }; }
		public static DimensionBounds Merge(DimensionBounds a, DimensionBounds b)
		{
			return a.IsEmpty ? b : b.IsEmpty ? a :
				a.FlippedOrder
				? new DimensionBounds { Start = Math.Max(a.Max, b.Max), End = Math.Min(a.Min, b.Min) }
				: new DimensionBounds { Start = Math.Min(a.Min, b.Min), End = Math.Max(a.Max, b.Max) };
		}
		public static double MergeQuality(DimensionBounds a, DimensionBounds b) { double mergedLength = Merge(a, b).Length; return double.IsNaN(mergedLength) ? 0.0 : a.Length * b.Length / mergedLength / mergedLength; }

		public static bool operator ==(DimensionBounds a, DimensionBounds b) { return a.Start == b.Start && a.End == b.End; }
		public static bool operator !=(DimensionBounds a, DimensionBounds b) { return a.Start != b.Start || a.End != b.End; }
		public override int GetHashCode() { return base.GetHashCode(); }
		public override bool Equals(object obj) { return obj is DimensionBounds && this == (DimensionBounds)obj; }
	}


	public class DimensionBoundsConverter : TypeConverter
	{
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string strval = value as string;
			if (strval == null) return null;
			var parameters = (from segment in strval.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
							  select segment.ParseAsDouble()
							 ).ToArray();
			if (parameters.Length < 1 || parameters.Length > 2 || parameters.Contains(null)) return null;

			return new DimensionBounds { Start = parameters[0].Value, End = parameters[parameters.Length - 1].Value };
		}
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType != typeof(string))
				return null;
			if (value == null || !(value is DimensionBounds))
				return null;
			DimensionBounds dim = (DimensionBounds)value;
			return dim.Start == dim.End ? dim.Start.ToString(culture) : dim.Start.ToString(culture) + "," + dim.End.ToString(culture);
		}
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType.Equals(typeof(string)) || base.CanConvertFrom(context, sourceType);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType.Equals(typeof(string)) || base.CanConvertTo(context, destinationType);
		}

	}

}
