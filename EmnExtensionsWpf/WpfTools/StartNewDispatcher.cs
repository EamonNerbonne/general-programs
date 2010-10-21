using System.Threading;
using System.Windows.Threading;

namespace EmnExtensions.Wpf {
	public static partial class WpfTools {
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
