using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using EmnExtensions.Text;

namespace EmnExtensions.Wpf.Plot
{
    [DebuggerDisplay("{AtStart} [] {AtEnd}")]
    [TypeConverter(typeof(DimensionMarginsConverter))]
    public struct DimensionMargins
    {
        public double AtStart { get; set; }
        public double AtEnd { get; set; }

        public double Sum
            => AtStart + AtEnd;

        public static DimensionMargins FromThicknessX(Thickness thickness)
            => new() { AtStart = thickness.Left, AtEnd = thickness.Right };

        public static DimensionMargins FromThicknessY(Thickness thickness)
            => new() { AtStart = thickness.Top, AtEnd = thickness.Bottom };

        public static DimensionMargins Merge(DimensionMargins a, DimensionMargins b)
            => new() { AtStart = Math.Max(a.AtStart, b.AtStart), AtEnd = Math.Max(a.AtEnd, b.AtEnd) };

        public static DimensionMargins Empty
            => new() { AtStart = 0.0, AtEnd = 0.0 };

        public static bool operator ==(DimensionMargins a, DimensionMargins b)
            => a.AtStart == b.AtStart && a.AtEnd == b.AtEnd;

        public static bool operator !=(DimensionMargins a, DimensionMargins b)
            => a.AtStart != b.AtStart || a.AtEnd != b.AtEnd;

        public override int GetHashCode()
            => base.GetHashCode();

        public override bool Equals(object obj)
            => obj is DimensionMargins margins && this == margins;
    }

    public sealed class DimensionMarginsConverter : TypeConverter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var strval = value as string;
            if (strval == null) {
                return null;
            }

            var parameters = (
                from segment in strval.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                select segment.ParseAsDouble()
            ).ToArray();
            if (parameters.Length < 1 || parameters.Length > 2 || parameters.Contains(null)) {
                return null;
            }

            return new DimensionMargins { AtStart = parameters[0].Value, AtEnd = parameters[^1].Value };
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType != typeof(string) || !(value is DimensionMargins dim)) {
                return null;
            }

            return dim.AtEnd == dim.AtStart ? dim.AtStart.ToString(culture) : dim.AtStart.ToString(culture) + "," + dim.AtEnd.ToString(culture);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }
}
