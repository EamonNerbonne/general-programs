using System;
using System.Globalization;

namespace EmnExtensions.Text
{
    public static class ParseString
    {
        public static DateTime? ParseAsDateTime(this string s)
        {
            if (DateTime.TryParse(s, out var val)) {
                return val;
            }

            return null;
        }

        public static DateTime? ParseAsDateTime(this string s, DateTimeStyles style, IFormatProvider provider)
        {
            if (DateTime.TryParse(s, provider, style, out var val)) {
                return val;
            }

            return null;
        }

        public static int? ParseAsInt32(this string s)
        {
            if (int.TryParse(s, out var val)) {
                return val;
            }

            return null;
        }

        public static int? ParseAsInt32(this string s, NumberStyles style, IFormatProvider provider)
        {
            if (int.TryParse(s, style, provider, out var val)) {
                return val;
            }

            return null;
        }

        public static ulong? ParseAsUInt64(this string s)
        {
            if (ulong.TryParse(s, out var val)) {
                return val;
            }

            return null;
        }

        public static ulong? ParseAsUInt64(this string s, NumberStyles style, IFormatProvider provider)
        {
            if (ulong.TryParse(s, style, provider, out var val)) {
                return val;
            }

            return null;
        }

        public static long? ParseAsInt64(this string s)
        {
            if (long.TryParse(s, out var val)) {
                return val;
            }

            return null;
        }

        public static long? ParseAsInt64(this string s, NumberStyles style, IFormatProvider provider)
        {
            if (long.TryParse(s, style, provider, out var val)) {
                return val;
            }

            return null;
        }

        public static double? ParseAsDouble(this string s)
        {
            if (double.TryParse(s, out var val)) {
                return val;
            }

            return null;
        }

        public static double? ParseAsDouble(this string s, NumberStyles style, IFormatProvider provider)
        {
            if (double.TryParse(s, style, provider, out var val)) {
                return val;
            }

            return null;
        }
    }
}
