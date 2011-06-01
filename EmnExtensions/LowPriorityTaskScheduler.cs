#define FIFO_TASKS
#define FIFO_THREADS
//#define DEBUG_TRACK_ITEMS
//best perf seems to be: fifo tasks&threads, no debug-tracking.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EmnExtensions {
	public sealed class LowPriorityTaskScheduler : TaskScheduler {
		[ThreadStatic]
		static bool isWorkerThread;
		sealed class WorkerThread {
#if DEBUG_TRACK_ITEMS
			public int normalCount;
#endif
			readonly LowPriorityTaskScheduler owner;
			readonly SemaphoreSlim sem = new SemaphoreSlim(1);
			bool shouldExit;
			public WorkerThread(LowPriorityTaskScheduler owner, ThreadPriority priority) {
				this.owner = owner;
				new Thread(DoWork) { IsBackground = true, Name = "LowPriorityTaskScheduler:" + priority, Priority = priority }.Start();
			}
			void DoWork() {
				isWorkerThread = true;
				while (true) {
					if (!sem.Wait(owner.IdleAfterMilliseconds)) owner.TerminateThread();//idle for 10 seconds, terminate a thread.
					else if (!shouldExit) owner.ProcessTask(this);//got signal, wasn't exit signal... go!
					else {//termination signal
						sem.Dispose();
#if DEBUG_TRACK_ITEMS
						PrintStats(true);
#endif
						break;
					}
				}
			}
			public void WakeThread() { sem.Release(); }
			public void ExitThread() { shouldExit = true; sem.Release(); }
#if DEBUG_TRACK_ITEMS
			public void PrintStats(bool exit=false) { Console.WriteLine((exit?"OnExit: ":"Current: ")+"Executed: "+ normalCount); }
#endif
		}

#if FIFO_TASKS
		readonly ConcurrentQueue<Task> tasks = new ConcurrentQueue<Task>();
		void AddTaskToQueue(Task task) { tasks.Enqueue(task); }
		bool TryGetQueuedTask(out Task retval) { return tasks.TryDequeue(out retval); }
		bool TasksAreQueued() { return tasks.Count > 0; }
#else
		readonly ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();
		void AddTaskToQueue(Task task) { tasks.Add(task); }
		bool TryGetQueuedTask(out Task retval) { return tasks.TryTake(out retval); }
		bool TasksAreQueued() { return tasks.Count > 0; }
#endif

#if FIFO_THREADS
		readonly ConcurrentQueue<WorkerThread> threads = new ConcurrentQueue<WorkerThread>();
		bool TryGetThread(out WorkerThread thread) { return threads.TryDequeue(out thread); }
		void AddThread(WorkerThread thread) { threads.Enqueue(thread); }
#else
		readonly ConcurrentBag<WorkerThread> threads = new ConcurrentBag<WorkerThread>();
		bool TryGetThread(out WorkerThread thread) { return threads.TryTake(out thread); }
		void AddThread(WorkerThread thread) { threads.Add(thread); }
#endif



		readonly int MaxParallel;
		readonly ThreadPriority Priority;
		readonly int IdleAfterMilliseconds;
		int currPar;
		public LowPriorityTaskScheduler(int? maxParallelism = null, ThreadPriority priority = ThreadPriority.Lowest, int? idleMilliseconds = null) {
			MaxParallel = maxParallelism ?? Environment.ProcessorCount * 2;
			Priority = priority;
			IdleAfterMilliseconds = idleMilliseconds ?? 10000;
		}

		public int WorkerCountEstimate { get { return currPar; } }
		public int IdleWorkerCountEstimate { get { return threads.Count; } }

		void WakeAnyThread() {
			WorkerThread idleThread;
			if (TryGetThread(out idleThread))
				idleThread.WakeThread();
			else {
				if (Interlocked.Increment(ref currPar) <= MaxParallel)
					new WorkerThread(this, Priority);
				else
					Interlocked.Decrement(ref currPar);
			}
		}

		void TerminateThread() {
			WorkerThread idleThread;
			if (TryGetThread(out idleThread)) {
				Interlocked.Decrement(ref currPar);
				idleThread.ExitThread();
				if (TasksAreQueued()) WakeAnyThread();//unlikely, but to guarrantee liveness.
			}
		}

#if DEBUG_TRACK_ITEMS
		int inlined;
		public void PrintCurrentStats() {
			foreach (WorkerThread t in threads)
				t.PrintStats();
			Console.WriteLine("Inlined: " + inlined);
		}
#endif

		protected override IEnumerable<Task> GetScheduledTasks() { return tasks; }

		protected override void QueueTask(Task task) { AddTaskToQueue(task); WakeAnyThread(); }

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
			bool okInline = isWorkerThread;// || Thread.CurrentThread.Priority >= Priority && IdleWorkerCountEstimate > 0;
			if (okInline) {
#if DEBUG_TRACK_ITEMS
				Interlocked.Increment(ref inlined);
#endif
				return TryExecuteTask(task);
			} else
				return false;
		}

		// ReSharper disable UnusedParameter.Local
		void SlipstreamQueueExecute(WorkerThread t) {
			// ReSharper restore UnusedParameter.Local
			Task another;
			while (TryGetQueuedTask(out another)) {
				TryExecuteTask(another);
#if DEBUG_TRACK_ITEMS
				t.normalCount++;
#endif
			}
		}

		void ProcessTask(WorkerThread t) {
			try {
				SlipstreamQueueExecute(t);
				//X: after slipstream, a new task *could* be added
			} finally {
				AddThread(t);
				//Y:now all threads *could* be halted leaving task added at X in limbo
				if (TasksAreQueued()) WakeAnyThread();//...so we need to ensure liveness.
				//if task is added @ Y or later, no limbo possible since thread was returned to pool.
			}
		}

		public override int MaximumConcurrencyLevel { get { return MaxParallel; } }

		static readonly LowPriorityTaskScheduler singleton = new LowPriorityTaskScheduler();
		public static LowPriorityTaskScheduler DefaultLowPriorityScheduler { get { return singleton; } }
	}
}
