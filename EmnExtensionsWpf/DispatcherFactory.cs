using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Threading;
using System.Reflection;

namespace EmnExtensions.Wpf {
	public static class DispatcherFactory {
		public static Dispatcher StartNewDispatcher() {
			using (var sem = new SemaphoreSlim(0)) {
				Dispatcher retval = null;
				var winThread = new Thread(() => {
					retval = Dispatcher.CurrentDispatcher;
					sem.Release();
					Dispatcher.Run();
				}) { IsBackground = true };
				winThread.SetApartmentState(ApartmentState.STA);
				winThread.Start();
				sem.Wait();
				
				return retval;
			}
		}
	}
}
