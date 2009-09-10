using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.Algorithms
{
	public static class Levenshtein
	{
		//modified from:http://www.merriampark.com/ldcsharp.htm
		public static int LevenshteinDistance(this string s, string t) {
			int n = s.Length; //length of s
			int m = t.Length; //length of t
			int[,] d = new int[n + 1, m + 1]; // matrix
			int cost; // cost
			// Step 1
			if (n == 0) return m;
			if (m == 0) return n;
			// Step 2
			for (int i = 0; i <= n; d[i, 0] = i++) ;
			for (int j = 0; j <= m; d[0, j] = j++) ;
			// Step 3
			for (int i = 0; i < n; i++) {
				//Step 4
				for (int j = 0; j < m; j++) {
					// Step 5
					cost = (t[j] == s[i] ? 0 : 2);//substitution will be cost 2.
					// Step 6
					d[i + 1, j + 1] = System.Math.Min(System.Math.Min(d[i, j + 1] + 1, d[i + 1, j] + 1), d[i, j] + cost);
				}
			}
			// Step 7
			return d[n, m];
		}
		public static double LevenshteinDistanceScaled(this string s, string t) {
			return LevenshteinDistance(s, t) / (double)Math.Max(1,Math.Max(s.Length, t.Length));
		}
	}
}
