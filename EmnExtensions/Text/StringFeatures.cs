namespace EmnExtensions.Text
{
    public static class StringFeatures
    {
        public static bool IsNullOrEmpty(this string str)
            => string.IsNullOrEmpty(str);

        public static string SubstringAfterFirst(this string haystack, string needle)
        {
            var needleIdx = haystack.IndexOf(needle);
            return needleIdx == -1 ? null : haystack.Substring(needleIdx + needle.Length);
        }

        public static string SubstringBeforeLast(this string haystack, string needle)
        {
            var needleIdx = haystack.LastIndexOf(needle);
            return needleIdx == -1 ? null : haystack.Substring(0, needleIdx);
        }

        public static string SubstringAfterAll(this string haystack, string needle)
        {
            var needleIdx = haystack.LastIndexOf(needle);
            return needleIdx == -1 ? haystack : haystack.Substring(needleIdx + needle.Length);
        }

        public static string SubstringUntil(this string haystack, string needle)
        {
            var needleIdx = haystack.IndexOf(needle);
            return needleIdx == -1 ? haystack : haystack.Substring(0, needleIdx);
        }
    }
}
