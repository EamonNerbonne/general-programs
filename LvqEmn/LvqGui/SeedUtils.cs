using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using EmnExtensions.MathHelpers;

namespace LvqGui {
	public static class SeedUtils {
		public static uint[] MakeInstSeed(this IHasSeed hasSeed, LvqWindowValues owner) {
			return new uint[] { hasSeed.Seed, owner.AppSettingsValues.GlobalInstSeed };
		}

		public static uint[] MakeParamsSeed(this IHasSeed hasSeed, LvqWindowValues owner) {
			return new uint[] { hasSeed.Seed, owner.AppSettingsValues.GlobalParamSeed };
		}

		public static DispatcherOperation BeginInvoke(this Dispatcher d, Action  action) {
			return d.BeginInvoke((Delegate)action);
			
		}

		public static DispatcherOperation BeginInvoke<T>(this Dispatcher d, Action<T> action, T param) {
			return d.BeginInvoke((Delegate)action, param);
		}

	}
}
