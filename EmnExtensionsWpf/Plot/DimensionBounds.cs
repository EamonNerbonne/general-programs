// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using EmnExtensions.Text;


namespace EmnExtensions.Wpf
{
    [DebuggerDisplay("[{Start}, {End}]")]
    [TypeConverter(typeof(DimensionBoundsConverter))]
    public struct DimensionBounds
    {
        public double Start { get; set; }
        public double End { get; set; }
        public bool FlippedOrder => End < Start;
        public double Min => Math.Min(Start, End);
        public double Max => Math.Max(Start, End);
        public bool IsEmpty => double.IsPositiveInfinity(Start) && double.IsPositiveInfinity(End);
        public double Length => Math.Abs(End - Start);

        public bool EncompassesValue(double value) => value >= Start && value <= End || value >= End && value <= Start;
        public bool HasNoOverlap(DimensionBounds other) => Max < other.Min || Min > other.Max;

        public DimensionBounds UnionWith(params double[] vals) => UnionWith(vals.AsEnumerable());
        public DimensionBounds UnionWith(IEnumerable<double> vals)
        {
            double min = Min, max = Max;
            if (IsEmpty) {
                max = double.NegativeInfinity;
            }

            foreach (var val in vals) {
                if (val < min) {
                    min = val;
                }

                if (val > max) {
                    max = val;
                }
            }

            return double.IsNegativeInfinity(max)
                    ? Empty
                    : FlippedOrder ? new DimensionBounds { Start = max, End = min } : new DimensionBounds { Start = min, End = max };
        }

        public void Translate(double offset) { Start += offset; End += offset; }
        public void Scale(double factor) { Start *= factor; End *= factor; }

        public static DimensionBounds Empty => new DimensionBounds { Start = double.PositiveInfinity, End = double.PositiveInfinity };
        public static DimensionBounds FromRectX(Rect r) => r.IsEmpty ? Empty : new DimensionBounds { Start = r.X, End = r.Right };
        public static DimensionBounds FromRectY(Rect r) => r.IsEmpty ? Empty : new DimensionBounds { Start = r.Y, End = r.Bottom };
        public static DimensionBounds Merge(DimensionBounds a, DimensionBounds b) => a.IsEmpty ? b : b.IsEmpty ? a :
                a.FlippedOrder
                ? new DimensionBounds { Start = Math.Max(a.Max, b.Max), End = Math.Min(a.Min, b.Min) }
                : new DimensionBounds { Start = Math.Min(a.Min, b.Min), End = Math.Max(a.Max, b.Max) };
        public static double MergeQuality(DimensionBounds a, DimensionBounds b) { var mergedLength = Merge(a, b).Length; return double.IsNaN(mergedLength) ? 0.0 : a.Length * b.Length / mergedLength / mergedLength; }

        public static bool operator ==(DimensionBounds a, DimensionBounds b) => a.Start == b.Start && a.End == b.End;
        public static bool operator !=(DimensionBounds a, DimensionBounds b) => a.Start != b.Start || a.End != b.End;
        public override int GetHashCode() => base.GetHashCode();
        public override bool Equals(object obj) => obj is DimensionBounds && this == (DimensionBounds)obj;

        internal void ScaleFromCenter(double p)
        {
            var midpoint = (Start + End) / 2;
            var dev = (End - Start) / 2;
            Start = midpoint - dev * p;
            End = midpoint + dev * p;
        }
    }


    public class DimensionBoundsConverter : TypeConverter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var strval = value as string;
            if (strval == null) {
                return null;
            }

            var parameters = (from segment in strval.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) select segment.ParseAsDouble()).ToArray();
            if (parameters.Length != 1 && parameters.Length != 2 || parameters.Contains(null)) {
                return null;
            }

            return new DimensionBounds { Start = parameters[0].Value, End = parameters[parameters.Length - 1].Value };
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType != typeof(string) || !(value is DimensionBounds)) {
                return null;
            }

            var dim = (DimensionBounds)value;
            return dim.Start == dim.End ? dim.Start.ToString(culture) : dim.Start.ToString(culture) + "," + dim.End.ToString(culture);
        }
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType.Equals(typeof(string)) || base.CanConvertFrom(context, sourceType);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType.Equals(typeof(string)) || base.CanConvertTo(context, destinationType);

    }

}
