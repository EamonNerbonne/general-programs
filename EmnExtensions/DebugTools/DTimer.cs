using System;
using System.Diagnostics;

namespace EmnExtensions.DebugTools {
	public sealed class DTimer : IDisposable {
		public DTimer(string actionLabel) { Start(actionLabel); }
		public DTimer(Action<TimeSpan> resultSink) { Start(resultSink); }

		public void NextAction(string newActionLabel) { Stop(); Start(newActionLabel); }
		public void NextAction(Action<TimeSpan> nextResultSink) { Stop(); Start(nextResultSink); }

		public void Start(string actionLabel) { Start(ts => Console.WriteLine("{0} took {1}", actionLabel, ts)); }
		public void Start(Action<TimeSpan> resultSink) { m_resultSink = resultSink; underlyingTimer = Stopwatch.StartNew(); }

		public void Stop() { underlyingTimer.Stop(); m_resultSink(underlyingTimer.Elapsed); }

		void IDisposable.Dispose() { underlyingTimer.Stop(); m_resultSink(underlyingTimer.Elapsed); }

		Action<TimeSpan> m_resultSink;
		Stopwatch underlyingTimer;
		public static T TimeFunc<T>(Func<T> f, string actionLabel) { using (new DTimer(actionLabel)) return f(); }
		public static T TimeFunc<T>(Func<T> f, Action<TimeSpan> resultSink) { using (new DTimer(resultSink)) return f(); }
		public static T TimeFunc<T, M>(Func<T> f, M key, Action<M, TimeSpan> resultsSink) { using (new DTimer(t => resultsSink(key, t))) return f(); }
		public static TimeSpan BenchmarkAction(Action a, int repeats) {
			long[] times = new long[repeats];
			for (int i = 0; i < repeats; i++) {
				Stopwatch w = Stopwatch.StartNew();
				a();
				times[i] = w.ElapsedTicks;
			}
			Array.Sort(times);
			return new TimeSpan(times[repeats / 4]);
		}
		public static TimeSpan TimeAction(Action a) { var w = Stopwatch.StartNew(); a(); return w.Elapsed; }
	}

	public static class DTimerExtensions {
		public static T TimeFunc<T>(this Func<T> f, string actionLabel) { return DTimer.TimeFunc(f, actionLabel); }
		public static T TimeFunc<T>(this Func<T> f, Action<TimeSpan> resultSink) { return DTimer.TimeFunc(f, resultSink); }
		public static T TimeFunc<T, M>(this Func<T> f, M key, Action<M, TimeSpan> resultsSink) { return DTimer.TimeFunc(f, key, resultsSink); }
	}
}
