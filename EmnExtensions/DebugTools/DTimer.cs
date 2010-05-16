using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace EmnExtensions.DebugTools
{
	public sealed class DTimer : IDisposable
	{
		public DTimer(string actionLabel) { Start(actionLabel); }
		public DTimer(Action<TimeSpan> resultSink) { Start(resultSink); }

		public void NextAction(string newActionLabel) { Stop(); Start(newActionLabel); }
		public void NextAction(Action<TimeSpan> nextResultSink) { Stop(); Start(nextResultSink); }

		public void Start(string actionLabel) { Start((ts) => { Console.WriteLine("{0} took {1}", actionLabel, ts); }); }
		public void Start(Action<TimeSpan> resultSink) { this.resultSink = resultSink; underlyingTimer = Stopwatch.StartNew(); }

		public void Stop() { underlyingTimer.Stop(); resultSink(underlyingTimer.Elapsed); }

		void IDisposable.Dispose() { underlyingTimer.Stop(); resultSink(underlyingTimer.Elapsed); }


		Action<TimeSpan> resultSink;
		Stopwatch underlyingTimer;


	}
	public static class Timer {
		public static T TimeFunc<T>(this Func<T> f, string actionLabel) { using (new DTimer(actionLabel)) return f(); }
		public static T TimeFunc<T>(this Func<T> f, Action<TimeSpan> resultSink) { using (new DTimer(resultSink)) return f(); }
		public static T TimeFunc<T, M>(this Func<T> f, M key, Action<M, TimeSpan> resultsSink) { using (new DTimer(t => { resultsSink(key, t); })) return f(); }
	}
}
