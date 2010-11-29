using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EmnExtensions {
	public sealed class LowPriorityTaskScheduler : TaskScheduler {
		sealed class WorkerThread : IDisposable {
			volatile Task todo;
			readonly Thread thread;
			readonly LowPriorityTaskScheduler owner;
			readonly SemaphoreSlim sem = new SemaphoreSlim(1);
			public WorkerThread(LowPriorityTaskScheduler owner) {
				this.owner = owner;
				thread = new Thread(DoWork) { IsBackground = true, Priority = ThreadPriority.Lowest };
			}
			void DoWork() {
				while (true) {
					sem.Wait();
					Task next = todo;
					todo = null;
					owner.ProcessTask(this, next);
				}
			}
			public void Start(Task task) {
				todo = task;
				thread.Start();
			}
			public void DoTask(Task task) {
				todo = task;
				sem.Release();
			}

			public void Dispose() {
				sem.Dispose();
			}
		}
		readonly ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();

		readonly ConcurrentBag<WorkerThread> threads = new ConcurrentBag<WorkerThread>();
		const int MaxParallel = 6;
		int currPar = 0;

		void DoTask(Task t) {
			WorkerThread idleThread;
			if (threads.TryTake(out idleThread))
				idleThread.DoTask(t);
			else {
				if (Interlocked.Increment(ref currPar) < MaxParallel) {
					new WorkerThread(this).Start(t);
				} else {
					Interlocked.Decrement(ref currPar);
					//ok, there was no workerthread freee, and we're at max capacity, so queue!
					tasks.Add(t);
					//possible that workerthread became free since check:
					if (threads.TryTake(out idleThread)) {
						Task fromQ;
						if (tasks.TryTake(out fromQ))
							idleThread.DoTask(fromQ);
						else
							threads.Add(idleThread);
					} //if not, then we're OK since eventually one will and then check the queue.
				}
			}
		}

		private void DoQueueTask() {
			Task fromQ;
			if (!tasks.TryTake(out fromQ)) return;
			//no need to start new workers, after all, all are made if something's in the queue.
			WorkerThread idleThread;
			if (threads.TryTake(out idleThread))
				idleThread.DoTask(fromQ);
			else
				tasks.Add(fromQ);
		}

		protected override IEnumerable<Task> GetScheduledTasks() { return tasks; }

		protected override void QueueTask(Task task) { DoTask(task); }

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
			return TryExecuteTask(task);
		}

		void ProcessTask(WorkerThread t, Task todo) {
			try {
				TryExecuteTask(todo);
				Task another;
				while (tasks.TryTake(out another))
					TryExecuteTask(another);
			} finally {
				threads.Add(t);
				//it's unlikely the queue is non-empty now, but possible.
				//to be sure of progress, if queue is nonempty ensure that there are worker threads in flight
				//we do that be dequeueing and running an item on a thread.  Since after the thread is returned to the pool the check is
				//made, we either have a thread available and will run the item (progress) or no thread available  - i.e. the just returned 
				//thread is already doing something - progress.
				DoQueueTask();
			}
		}

		public override int MaximumConcurrencyLevel { get { return MaxParallel; } }
		static readonly LowPriorityTaskScheduler singleton = new LowPriorityTaskScheduler();
		public static LowPriorityTaskScheduler Instance { get { return singleton; } }
	}
}
