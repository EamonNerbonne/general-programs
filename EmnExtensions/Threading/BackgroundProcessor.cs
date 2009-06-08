using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace EmnExtensions.Threading
{
	public static class BackgroundProcessor //TODO this shouldn't use semaphores but mutually incremented modulo counters
	{
		class ProcHelp<T> : IDisposable
		{
			Semaphore readS;
			Semaphore writeS;
			bool cancelled = false;
			IEnumerable<T> orig;
			T[] buffer;
			int readPos = 0, writePos = 0, queueDepth;
			public ProcHelp(IEnumerable<T> orig, int queueDepth) {
				if (queueDepth < 1) throw new ArgumentException("You cannot make a background buffer of size 0;");
				readS = new Semaphore(0, queueDepth + 1);//+1 is for cancelling.
				writeS = new Semaphore(queueDepth, queueDepth + 1);
				this.orig = orig;
				this.queueDepth = queueDepth;
				buffer = new T[queueDepth];
			}
			public void BackgroundRun(object ignored) {
				BackgroundRun();
			}
			public void BackgroundRun() {
				using (var enumerator = orig.GetEnumerator()) {
					while (true) {
						writeS.WaitOne();
						if (cancelled) break;
						try {
							if (enumerator.MoveNext()) {
								lock (buffer) buffer[writePos] = enumerator.Current;
								writePos = (writePos + 1) % queueDepth; //writePos is bgThread local, no need to lock.
							} else {
								cancelled = true;
								break;
							}
						} catch {
							cancelled = true;
							throw;
						} finally {
							readS.Release(1);// we don't want the main thread to block - on error, empty or continue.
						}
					}
				}
			}

			public bool Generate(out T item) {
				readS.WaitOne();
				if (cancelled) {
					item = default(T);
					return false; //no need to increment writeS since the writer has already exited anyhow...
				} else {
					lock (buffer) item = buffer[readPos];
					readPos = (readPos + 1) % queueDepth;//readPos is Generate-thread local, no need to lock.
					writeS.Release(1);
					return true;
				}
			}
			public void Cancel() {
				cancelled = true;
				//if(bgThread.IsAlive) bgThread.Abort(); //some people claim Thread.Abort is nasty.  Commenting this line out will potentially allow some unnecessary computation, but not break anything.
				writeS.Release(1);
			}
			public void Dispose() {
				Cancel();
				GC.SuppressFinalize(this);
			}



		}
		public static IEnumerable<T> InAnotherThread<T>(this IEnumerable<T> orig) {
			return InAnotherThread(orig, 10);
		}

		public static IEnumerable<T> InAnotherThread<T>(this IEnumerable<T> orig, int queueDepth) {
#if DISABLETHREADING
            return orig;
#else
			using (ProcHelp<T> proc = new ProcHelp<T>(orig, queueDepth)) {
				ThreadPool.QueueUserWorkItem(proc.BackgroundRun);
				while (true) {
					int worker, cp, workerMax, cpMax;
					ThreadPool.GetAvailableThreads(out worker, out cp);
					if (worker > 0 && cp > 0) break;
					ThreadPool.GetMaxThreads(out workerMax, out cpMax);
					if (worker == 0) workerMax += 4;
					if (cp == 0) cpMax += 4;
					ThreadPool.SetMaxThreads(workerMax, cpMax);
				}
				//Thread t = new Thread(proc.BackgroundRun);
				//  t.IsBackground = true;
				// t.Start();
				while (true) {
					T item;
					bool hasNext = proc.Generate(out item);
					if (hasNext)
						yield return item;
					else
						yield break;
				}

			}
#endif
		}

	}
}
