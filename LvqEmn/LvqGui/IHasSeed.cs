using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.MathHelpers;

namespace LvqGui {
	public interface IHasSeed {
		uint Seed { get; set; }
		uint InstSeed { get; set; }
	}
	public static class IHasSeedExtensions {
		public static void ReseedBoth(this IHasSeed seededObj) { seededObj.ReseedParam(); seededObj.ReseedInst(); }
		public static void ReseedParam(this IHasSeed seededObj) { seededObj.Seed = RndHelper.MakeSecureUInt(); }
		public static void ReseedInst(this IHasSeed seededObj) { seededObj.InstSeed = RndHelper.MakeSecureUInt(); }
	}
}
