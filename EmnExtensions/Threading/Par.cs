using System;
using System.Threading;

namespace EmnExtensions.Threading
{
	public static class Par
	{
		public static void Invoke(params Action[] actions) {
			using (Semaphore sem = new Semaphore(0, actions.Length)) {
				foreach (Action action in actions) {
					var localaction = action;
					ThreadPool.QueueUserWorkItem(ignore => {
						try { localaction(); } finally { sem.Release(); }
					});
				}
				foreach (Action a in actions)
					sem.WaitOne();
			}
		}
	}
}
