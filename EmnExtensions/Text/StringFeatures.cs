﻿
namespace EmnExtensions.Text {
	public static class StringFeatures {
		public static bool IsNullOrEmpty(this string str) { return string.IsNullOrEmpty(str); }
		public static string SubstringAfter(this string haystack, string needle) {
			int needleIdx = haystack.IndexOf(needle);
			return needleIdx == -1 ? null : haystack.Substring(needleIdx + needle.Length);
		}
		public static string SubstringBefore(this string haystack, string needle) {
			int needleIdx = haystack.LastIndexOf(needle);
			return needleIdx == -1 ? null : haystack.Substring(0,needleIdx);
		}
	}
}
