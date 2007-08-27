using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EamonExtensionsLinq.Text
{
	public static class ParseString
	{
		public static DateTime? ParseAsDateTime(this string s) {
			DateTime val;
			if(DateTime.TryParse(s, out val)) return val;
			else return null;
		}
		public static int? ParseAsInt32(this string s) {
			int val;
			if(int.TryParse(s, out val)) return val;
			else return null;
		}

		public static ulong? ParseAsUInt64(this string s) {
			ulong val;
			if(ulong.TryParse(s, out val)) return val;
			else return null;
		}

		public static long? ParseAsInt64(this string s) {
			long val;
			if(long.TryParse(s, out val)) return val;
			else return null;
		}
	}
}
