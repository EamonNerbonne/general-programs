using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.MathHelpers;

namespace LvqGui {
	public interface IHasSeed {
		uint Seed { get; set; }
	}
	public static class IHasSeedExtensions {
		public static void Reseed(this IHasSeed seededObj) { seededObj.Seed = RndHelper.MakeSecureUInt(); }
	}
}
