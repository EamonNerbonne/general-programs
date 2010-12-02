using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using System.Threading.Tasks;
using System.Threading;
using EmnExtensions;

namespace EmnExtensionsTest {
	public class LowPriorityTaskSchedulerTest {
		TaskScheduler scheduler =
			//new LowPriorityTaskScheduler(8);
		//new LimitedConcurrencyLevelTaskScheduler(8);
		TaskScheduler.Default;

		//[Fact]
		public void EverythingRunsOnce() {
			int a = 0, b = 0, c = 0;
			int v1;

			const int count = 23456;

			Task<Task[]> subs1set = Task.Factory.StartNew(() =>
				Enumerable.Range(0, count).Select(i =>
					Task.Factory.StartNew(() => {
						if (Enumerable.Range(0, i + 1).Sum() % 2 == 0)
							Interlocked.Increment(ref a);
						else
							Interlocked.Decrement(ref a);
					}, CancellationToken.None, TaskCreationOptions.None, scheduler)
				).ToArray()
			, CancellationToken.None, TaskCreationOptions.None, scheduler);
			Task<Task[]> subs2set = Task.Factory.StartNew(() =>
				Enumerable.Range(0, count).Reverse().Select(i =>
					Task.Factory.StartNew(() => {
						if (Enumerable.Range(0, i + 1).Sum() % 3 == 0)
							Interlocked.Increment(ref b);
						else
							Interlocked.Decrement(ref b);
					}, CancellationToken.None, TaskCreationOptions.None, scheduler)
				).ToArray()
			, CancellationToken.None, TaskCreationOptions.None, scheduler);
			Task<Task[]> subs3set = Task.Factory.StartNew(() =>
				Enumerable.Range(0, count).Select(i =>
					Task.Factory.StartNew(() => {
						if (Enumerable.Range(0, i + 1).Sum() % 5 == 0)
							Interlocked.Increment(ref c);
						else
							Interlocked.Decrement(ref c);
					}, CancellationToken.None, TaskCreationOptions.None, scheduler)
				).ToArray()
			, CancellationToken.None, TaskCreationOptions.None, scheduler);

			Task.WaitAll(subs1set, subs2set, subs3set);
			Task.WaitAll(subs1set.Result);
			Task.WaitAll(subs2set.Result);
			Task.WaitAll(subs3set.Result);
			Assert.Equal((1 - ((count - 1) % 4)) % 2, a);
			Assert.Equal(count / 3 + (count % 3 == 1 ? 1 : 0), b);
			Assert.Equal(-(count / 5) + (count % 5 == 0 ? 0 : 2 - count % 5), c);
		}

		//[Fact]
		public void PlainPerf() {
			long[] vals = new long[500000];
			const long scale = 1;
			const int max = 8000;
			Parallel.For(0, vals.Length, new ParallelOptions { TaskScheduler = scheduler }, i => {
				long sum = 0;
				for (int j = 1; j <= i * scale % max; j++)
					sum += j;
				vals[i] = sum;
			});
			for (long j = 0; j < vals.Length; j++) {
				long i = j * scale % max;
				Assert.Equal((i * i * scale * scale + i * scale) / 2, vals[j]);
			}
		}
		[Fact]
		public void PlainPerfRaw() {
			Task<long>[] tasks = new Task<long>[1000000];
			const int scale = 1;
			const int max = 50000;
			for (int k = 0; k < tasks.Length; k++) {
				int i = k;
				tasks[k] = Task.Factory.StartNew(() => {
					long sum = 0;
					for (int j = 1; j <= i * scale % max; j++)
						sum += j;
					return sum;
				}, CancellationToken.None, TaskCreationOptions.None, scheduler);
			}
			//Task.WaitAll(tasks);
			for (int j = 0; j < tasks.Length; j++) {
				int i = j * scale % max;
				Assert.Equal((i * (long)i * scale * scale + i * scale) / 2, tasks[j].Result);
			}
			//if (scheduler is LowPriorityTaskScheduler) 				(scheduler as LowPriorityTaskScheduler).PrintCurrentStats();
		}

		//[Fact]
		public void CheckExit() {
			LowPriorityTaskScheduler typedScheduler = scheduler as LowPriorityTaskScheduler;
			if (typedScheduler == null) return;
			Assert.True(0 == typedScheduler.WorkerCountEstimate, "there are workers");
			PlainPerfRaw();
			Assert.Equal(8, typedScheduler.IdleWorkerCountEstimate);
			Assert.Equal(8, typedScheduler.WorkerCountEstimate);
			Thread.Sleep(10100);
			Assert.True(0 == typedScheduler.WorkerCountEstimate, "there are workers");



		}

	}

	//from http://msdn.microsoft.com/en-us/library/ee789351.aspx
	public class LimitedConcurrencyLevelTaskScheduler : TaskScheduler {
		/// <summary>Whether the current thread is processing work items.</summary>
		[ThreadStatic]
		private static bool _currentThreadIsProcessingItems;
		/// <summary>The list of tasks to be executed.</summary>
		private readonly LinkedList<Task> _tasks = new LinkedList<Task>(); // protected by lock(_tasks)
		/// <summary>The maximum concurrency level allowed by this scheduler.</summary>
		private readonly int _maxDegreeOfParallelism;
		/// <summary>Whether the scheduler is currently processing work items.</summary>
		private int _delegatesQueuedOrRunning = 0; // protected by lock(_tasks)

		/// <summary>
		/// Initializes an instance of the LimitedConcurrencyLevelTaskScheduler class with the
		/// specified degree of parallelism.
		/// </summary>
		/// <param name="maxDegreeOfParallelism">The maximum degree of parallelism provided by this scheduler.</param>
		public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism) {
			if (maxDegreeOfParallelism < 1) throw new ArgumentOutOfRangeException("maxDegreeOfParallelism");
			_maxDegreeOfParallelism = maxDegreeOfParallelism;
		}

		/// <summary>Queues a task to the scheduler.</summary>
		/// <param name="task">The task to be queued.</param>
		protected sealed override void QueueTask(Task task) {
			// Add the task to the list of tasks to be processed.  If there aren't enough
			// delegates currently queued or running to process tasks, schedule another.
			lock (_tasks) {
				_tasks.AddLast(task);
				if (_delegatesQueuedOrRunning < _maxDegreeOfParallelism) {
					++_delegatesQueuedOrRunning;
					NotifyThreadPoolOfPendingWork();
				}
			}
		}

		/// <summary>
		/// Informs the ThreadPool that there's work to be executed for this scheduler.
		/// </summary>
		private void NotifyThreadPoolOfPendingWork() {
			ThreadPool.UnsafeQueueUserWorkItem(_ => {
				// Note that the current thread is now processing work items.
				// This is necessary to enable inlining of tasks into this thread.
				_currentThreadIsProcessingItems = true;
				try {
					// Process all available items in the queue.
					while (true) {
						Task item;
						lock (_tasks) {
							// When there are no more items to be processed,
							// note that we're done processing, and get out.
							if (_tasks.Count == 0) {
								--_delegatesQueuedOrRunning;
								break;
							}

							// Get the next item from the queue
							item = _tasks.First.Value;
							_tasks.RemoveFirst();
						}

						// Execute the task we pulled out of the queue
						base.TryExecuteTask(item);
					}
				}
					// We're done processing items on the current thread
				finally { _currentThreadIsProcessingItems = false; }
			}, null);
		}

		/// <summary>Attempts to execute the specified task on the current thread.</summary>
		/// <param name="task">The task to be executed.</param>
		/// <param name="taskWasPreviouslyQueued"></param>
		/// <returns>Whether the task could be executed on the current thread.</returns>
		protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
			// If this thread isn't already processing a task, we don't support inlining
			if (!_currentThreadIsProcessingItems) return false;

			// If the task was previously queued, remove it from the queue
			if (taskWasPreviouslyQueued) TryDequeue(task);

			// Try to run the task.
			return base.TryExecuteTask(task);
		}

		/// <summary>Attempts to remove a previously scheduled task from the scheduler.</summary>
		/// <param name="task">The task to be removed.</param>
		/// <returns>Whether the task could be found and removed.</returns>
		protected sealed override bool TryDequeue(Task task) {
			lock (_tasks) return _tasks.Remove(task);
		}

		/// <summary>Gets the maximum concurrency level supported by this scheduler.</summary>
		public sealed override int MaximumConcurrencyLevel { get { return _maxDegreeOfParallelism; } }

		/// <summary>Gets an enumerable of the tasks currently scheduled on this scheduler.</summary>
		/// <returns>An enumerable of the tasks currently scheduled.</returns>
		protected sealed override IEnumerable<Task> GetScheduledTasks() {
			bool lockTaken = false;
			try {
				Monitor.TryEnter(_tasks, ref lockTaken);
				if (lockTaken) return _tasks.ToArray();
				else throw new NotSupportedException();
			} finally {
				if (lockTaken) Monitor.Exit(_tasks);
			}
		}
	}
}
