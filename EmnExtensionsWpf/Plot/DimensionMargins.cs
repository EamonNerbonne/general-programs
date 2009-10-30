using System;
using System.Windows;
using System.ComponentModel;
using System.Globalization;
using EmnExtensions.Text;
using System.Linq;

namespace EmnExtensions.Wpf.Plot
{
	[TypeConverter(typeof(DimensionMarginsConverter))]
	public struct DimensionMargins
	{
		public double AtStart { get; set; }
		public double AtEnd { get; set; }
		public double Sum { get { return AtStart + AtEnd; } }
		public static DimensionMargins FromThicknessX(Thickness thickness) { return new DimensionMargins { AtStart = thickness.Left, AtEnd = thickness.Right }; }
		public static DimensionMargins FromThicknessY(Thickness thickness) { return new DimensionMargins { AtStart = thickness.Top, AtEnd = thickness.Bottom }; }
		public static DimensionMargins Merge(DimensionMargins a, DimensionMargins b) { return new DimensionMargins { AtStart = Math.Max(a.AtStart, b.AtStart), AtEnd = Math.Max(a.AtEnd, b.AtEnd) }; }
		public static DimensionMargins Undefined { get { return new DimensionMargins { AtStart = double.NegativeInfinity, AtEnd = double.NegativeInfinity }; } }

		public static bool operator ==(DimensionMargins a, DimensionMargins b) { return a.AtStart == b.AtStart && a.AtEnd == b.AtEnd; }
		public static bool operator !=(DimensionMargins a, DimensionMargins b) { return a.AtStart != b.AtStart || a.AtEnd != b.AtEnd; }
		public override int GetHashCode() { return base.GetHashCode(); }
		public override bool Equals(object obj) { return obj is DimensionMargins && this == (DimensionMargins)obj; }
	}

	public class DimensionMarginsConverter : TypeConverter
	{
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
			string strval = value as string;
			if (strval == null) return null;
			var parameters = (from segment in strval.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
							  select segment.ParseAsDouble()
							 ).ToArray();
			if (parameters.Length < 1 || parameters.Length > 2 || parameters.Contains(null)) return null;

			return new DimensionMargins { AtStart = parameters[0].Value, AtEnd = parameters[parameters.Length - 1].Value };
		}
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
			if (destinationType != typeof(string))
				return null;
			if (value == null || !(value is DimensionMargins))
				return null;
			DimensionMargins dim= (DimensionMargins)value;
			return dim.AtEnd==dim.AtStart?dim.AtStart.ToString(culture):dim.AtStart.ToString(culture)+","+dim.AtEnd.ToString(culture);
		}
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
			return sourceType.Equals(typeof(string)) || base.CanConvertFrom(context, sourceType);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
			return destinationType.Equals(typeof(string)) || base.CanConvertTo(context, destinationType);
		}

	}
}
