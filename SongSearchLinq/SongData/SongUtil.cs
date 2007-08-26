using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Linq;
using EamonExtensionsLinq;
using System.IO;

namespace SongDataLib
{
	public static class SongUtil
	{


		public static int? StringToNullableInt(string num) {
			int retval;
			if(!int.TryParse(num, out retval)) return null; else return retval;
		}

		public static byte[] str2byteArr(string str) { return str.ToCharArray().Where(c => (int)c < 256).Select(c => (byte)c).ToArray(); }

		public static byte[] CanonicalizedSearchStr(string str) {
			return str2byteArr(EamonExtensionsLinq.Text.Canonicalize.Basic(str));
		}

		public static bool Contains(byte[] elem, byte[] substring) {
			for(int i = 0; i <= elem.Length - substring.Length; i++) {
				bool match = true;
				for(int j = 0; j < substring.Length; j++) {
					if(elem[i + j] != substring[j]) {
						match = false;
						break;
					}
				}
				if(match)
					return true;
			}
			return false;
		}


	}
}
