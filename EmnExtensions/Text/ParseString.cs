using System;
using System.Globalization;

namespace EmnExtensions.Text
{
	public static class ParseString
	{
		public static DateTime? ParseAsDateTime(this string s) {
			DateTime val;
			if(DateTime.TryParse(s, out val)) return val;
			else return null;
		}
		public static DateTime? ParseAsDateTime(this string s,DateTimeStyles style, IFormatProvider provider) {
			DateTime val;
			if (DateTime.TryParse(s,provider,style, out val)) return val;
			else return null;
		}


		public static int? ParseAsInt32(this string s) {
			int val;
			if(int.TryParse(s, out val)) return val;
			else return null;
		}
		public static int? ParseAsInt32(this string s, NumberStyles style, IFormatProvider provider) {
			int val;
			if (int.TryParse(s, style, provider, out val)) return val;
			else return null;
		}

		public static ulong? ParseAsUInt64(this string s) {
			ulong val;
			if(ulong.TryParse(s, out val)) return val;
			else return null;
		}
		public static ulong? ParseAsUInt64(this string s, NumberStyles style, IFormatProvider provider) {
			ulong val;
			if (ulong.TryParse(s, style, provider, out val)) return val;
			else return null;
		}

		public static long? ParseAsInt64(this string s) {
			long val;
			if(long.TryParse(s, out val)) return val;
			else return null;
		}
		public static long? ParseAsInt64(this string s, NumberStyles style, IFormatProvider provider) {
			long val;
			if (long.TryParse(s,style,provider, out val)) return val;
			else return null;
		}

		public static double? ParseAsDouble(this string s) {
			double val;
			if (Double.TryParse(s, out val)) return val;
			else return null;
		}
		public static double? ParseAsDouble(this string s, NumberStyles style, IFormatProvider provider ) {
			double val;
			if (Double.TryParse(s,style,provider,  out val)) return val;
			else return null;
		}

	}
}
