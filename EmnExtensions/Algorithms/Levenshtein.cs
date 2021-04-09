using System;

namespace EmnExtensions.Algorithms
{
    public static class Levenshtein
    {
        //modified from:http://www.merriampark.com/ldcsharp.htm
        public static int LevenshteinDistance(this string s, string t)
        {
            var sLen = s.Length; //length of s
            var tLen = t.Length; //length of t
            var d = new int[sLen + 1, tLen + 1]; // matrix
            for (var i = 0; i <= sLen; i++) {
                d[i, 0] = i;
            }

            for (var j = 0; j <= tLen; j++) {
                d[0, j] = j;
            }

            for (var i = 0; i < sLen; i++) {
                for (var j = 0; j < tLen; j++) {
                    var cost = (t[j] == s[i] ? 0 : 2); //substitution will be cost 2.
                    d[i + 1, j + 1] = Math.Min(Math.Min(d[i, j + 1] + 1, d[i + 1, j] + 1), d[i, j] + cost);
                }
            }

            return d[sLen, tLen];
        }

        public static double LevenshteinDistanceScaled(this string s, string t) => LevenshteinDistance(s, t) / (double)Math.Max(1, Math.Max(s.Length, t.Length));
    }
}
