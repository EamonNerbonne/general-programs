using System;
using System.Windows;
using System.ComponentModel;
using EmnExtensions.Text;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;


namespace EmnExtensions.Wpf.Plot
{
	[TypeConverter(typeof(DimensionBoundsConverter))]
	public struct DimensionBounds
	{
		public double Min { get; set; }
		public double Max { get; set; }
		public double Length { get { return Max - Min; } }
		public bool EncompassesValue(double value) { return value >= Min && value <= Max; }
		public DimensionBounds UnionWith(params double[] vals) { return UnionWith(vals.AsEnumerable()); }
		public DimensionBounds UnionWith(IEnumerable<double> vals) {
			DimensionBounds copy = this;
			foreach (double val in vals) {
				if (val < copy.Min)
					copy.Min = val;
				if (val > copy.Max)
					copy.Max = val;
			}
			return copy;
		} 

		public void Translate(double offset) { Min += offset; Max += offset; }
		public void Scale(double factor) { Min *= factor; Max *= factor; }

		public static DimensionBounds Undefined { get { return new DimensionBounds { Min = double.PositiveInfinity, Max = double.NegativeInfinity }; } }
		public static DimensionBounds FromRectX(Rect r) { return new DimensionBounds { Min = r.X, Max = r.Right }; }
		public static DimensionBounds FromRectY(Rect r) { return new DimensionBounds { Min = r.Y, Max = r.Bottom }; }
		public static DimensionBounds Merge(DimensionBounds a, DimensionBounds b) { return new DimensionBounds { Min = Math.Min(a.Min, b.Min), Max = Math.Max(a.Max, b.Max) }; }
		public static double MergeQuality(DimensionBounds a, DimensionBounds b) { double mergedLength = Merge(a, b).Length; return a.Length * b.Length / mergedLength / mergedLength; }
	}


	public class DimensionBoundsConverter : TypeConverter
	{
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
			string strval = value as string;
			if (strval == null) return null;
			var parameters = (from segment in strval.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
							  select segment.ParseAsDouble()
							 ).ToArray();
			if (parameters.Length < 1 || parameters.Length > 2 || parameters.Contains(null)) return null;

			return new DimensionBounds { Min = parameters[0].Value, Max = parameters[parameters.Length - 1].Value };
		}
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
			if (destinationType != typeof(string))
				return null;
			if (value == null || !(value is DimensionBounds))
				return null;
			DimensionBounds dim = (DimensionBounds)value;
			return dim.Min == dim.Max ? dim.Min.ToString(culture) : dim.Min.ToString(culture) + "," + dim.Max.ToString(culture);
		}
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
			return sourceType.Equals(typeof(string)) || base.CanConvertFrom(context, sourceType);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
			return destinationType.Equals(typeof(string)) || base.CanConvertTo(context, destinationType);
		}

	}

}
