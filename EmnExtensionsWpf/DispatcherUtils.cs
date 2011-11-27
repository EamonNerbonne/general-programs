using System;
using System.Linq;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Threading;

namespace LvqGui {
	public static class DispatcherUtils {
		public static DispatcherOperation BeginInvoke(this Dispatcher d, Action action) {
			return d.BeginInvoke(action);
		}

		public static DispatcherOperation BeginInvokeBackground(this Dispatcher d, Action action) {
			return d.BeginInvoke(action, DispatcherPriority.Background);
		}

		public static DispatcherOperation BeginInvoke<T>(this Dispatcher d, Action<T> action, T param) {
			return d.BeginInvoke(action, param);
		}

		public static Task AsTask(this DispatcherOperation op) {
			TaskCompletionSource<int> whenDone = new TaskCompletionSource<int>();
			op.Aborted += (s, e) => whenDone.SetCanceled();
			op.Completed += (s, e) => whenDone.SetResult(0);
			return whenDone.Task;
		}

		public static Task CompletedTask() {
			TaskCompletionSource<int> whenDone = new TaskCompletionSource<int>();
			whenDone.SetResult(0);
			return whenDone.Task;
		}

		public static Task<TaskScheduler> GetScheduler(this Dispatcher d) {
			TaskCompletionSource<TaskScheduler> schedulerResult = new TaskCompletionSource<TaskScheduler>();
			d.BeginInvoke(() => schedulerResult.SetResult(TaskScheduler.FromCurrentSynchronizationContext()));
			return schedulerResult.Task;
		}

		public static Task StartNewTask(this TaskScheduler scheduler,Action action,CancellationToken cancellationToken = default(CancellationToken), TaskCreationOptions creationOptions = TaskCreationOptions.None) {
			return Task.Factory.StartNew(action, cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken, creationOptions, scheduler);
		}

		public static Task<T> StartNewTask<T>(this TaskScheduler scheduler, Func<T> func, CancellationToken cancellationToken = default(CancellationToken), TaskCreationOptions creationOptions = TaskCreationOptions.None) {
			return Task.Factory.StartNew(func, cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken, creationOptions, scheduler);
		}
	}
}
