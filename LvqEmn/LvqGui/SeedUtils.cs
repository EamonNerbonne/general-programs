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

		public static Func<uint> MakeInstSeed(this IHasSeed hasSeed, LvqWindowValues owner) {
			return MakeSeedFunc(new uint[] { hasSeed.Seed, owner.AppSettingsValues.GlobalInstSeed });
		}

		public static Func<uint> MakeParamsSeed(this IHasSeed hasSeed, LvqWindowValues owner) {
			return MakeSeedFunc(new uint[] { hasSeed.Seed, owner.AppSettingsValues.GlobalParamSeed });
		}

	}
}
