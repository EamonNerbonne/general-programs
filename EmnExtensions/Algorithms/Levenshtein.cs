using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.Algorithms {
    public static class Levenshtein {
        //modified from:http://www.merriampark.com/ldcsharp.htm
        public static int LevenshteinDistance(this string s, string t) {
            int sLen = s.Length; //length of s
            int tLen = t.Length; //length of t
            int[,] d = new int[sLen + 1, tLen + 1]; // matrix
            for (int i = 0; i <= sLen; i++) d[i, 0] = i;
            for (int j = 0; j <= tLen; j++) d[0, j] = j;
            for (int i = 0; i < sLen; i++) {
                for (int j = 0; j < tLen; j++) {
                    var cost = (t[j] == s[i] ? 0 : 2);//substitution will be cost 2.
                    d[i + 1, j + 1] = Math.Min(Math.Min(d[i, j + 1] + 1, d[i + 1, j] + 1), d[i, j] + cost);
                }
            }
            return d[sLen, tLen];
        }
        public static double LevenshteinDistanceScaled(this string s, string t) {
            return LevenshteinDistance(s, t) / (double)Math.Max(1, Math.Max(s.Length, t.Length));
        }
    }
}
