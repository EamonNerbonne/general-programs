using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.Text;

namespace LastFMspider.FuzzySongSearcherInternal {
	internal static class Trigrammer {
		public static IEnumerable<uint> Trigrams(string input) {
			if (input == null)
				yield break;
			string canonicalized = Canonicalize.Basic(input);
			if (canonicalized.Length == 0)
				yield break;

			uint[] codes = canonicalized.PadLeft(3, (char)0xfffd).Select(c => CharMap.MapChar(c)).ToArray();
			for (int i = 0; i < codes.Length - 2; i++)
				yield return TrigramCode(codes[i], codes[i + 1], codes[i + 2]);
		}
		public static uint TrigramCode(uint a, uint b, uint c) { return a + b * CharMap.MapSize + c * CharMap.MapSize * CharMap.MapSize; }
		public static uint TrigramCount { get { return CharMap.MapSize * CharMap.MapSize * CharMap.MapSize; } }
	}
}
