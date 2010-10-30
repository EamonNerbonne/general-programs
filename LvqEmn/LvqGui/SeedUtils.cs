using System;
using System.Linq;
using System.Windows.Threading;

namespace LvqGui {
	public static class SeedUtils {

		public static DispatcherOperation BeginInvoke(this Dispatcher d, Action  action) {
			return d.BeginInvoke(action);
		}

		public static DispatcherOperation BeginInvokeBackground(this Dispatcher d, Action action) {
			return d.BeginInvoke(action,DispatcherPriority.Background);
		}


		public static DispatcherOperation BeginInvoke<T>(this Dispatcher d, Action<T> action, T param) {
			return d.BeginInvoke(action, param);
		}
	}
}
