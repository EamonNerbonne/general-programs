using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LvqGui {
	public static class SeedUtils {
		public static Func<uint> MakeSeedFunc(uint[] seeds) {
			int i = 0;
			return () => i < seeds.Length ? seeds[i++] : 0;
		}
	}
}
