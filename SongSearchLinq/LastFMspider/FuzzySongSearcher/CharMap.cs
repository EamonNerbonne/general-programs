using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.DebugTools;
using EmnExtensions.Text;

namespace LastFMspider.FuzzySongSearcherInternal {
	internal static class CharMap {
		static CharMap() {
			using (new DTimer("constructing map")) {
				uint[] retval = new uint[1 + (int)char.MaxValue]; //maps from char code to dense code.
				List<char> reverseMap = new List<char>();
				for (int i = 0; i < retval.Length; i++)
					try {
						foreach (char c in Canonicalize.Basic(new string(new[] { (char)i })))
							retval[(int)c]++;
					} catch (ArgumentException) { }// canonicalize may fail on invalid strings - i.e. chars with just
				//retval now contains the occurence count of a particular output charcode.
				reverseMap.Add((char)0xfffd);//0 == nonsense == mapped to replacement character;
				for (int i = 0; i < retval.Length; i++)
					if (i != 0xfffd && retval[i] > 0) { //0xfffd is already mapped, and if retval[i]==0, then it doesn't need mapping.
						retval[i] = (uint)reverseMap.Count;
						reverseMap.Add((char)i);
					}

				rawCharmap = retval;
				reverseCharmap = reverseMap.ToArray();
			}
		}

		static readonly uint[] rawCharmap;
		static readonly char[] reverseCharmap;
		public static uint MapChar(char c) { return rawCharmap[(int)c]; }
		public static char UnmapChar(uint i) { return reverseCharmap[i]; }
		/// <summary>
		/// if i is out of range, returns the unicode character 0xfffd (replacement char) instead of throwing an exception.
		/// </summary>
		public static char TryUnmapChar(uint i) { return i < reverseCharmap.Length ? reverseCharmap[i] : (char)0xfffd; }
		public static uint MapSize { get { return (uint)reverseCharmap.Length; } }

	}
}
