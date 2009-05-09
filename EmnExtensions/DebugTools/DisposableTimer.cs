using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace EmnExtensions.DebugTools
{
	public sealed class DisposableTimer : IDisposable
	{
		Action<TimeSpan> resultSink;
		Stopwatch underlyingTimer;
		public DisposableTimer(string actionLabel) : this((ts) => { Console.WriteLine("{0} took {1}", actionLabel, ts); }) { }

		public DisposableTimer(Action<TimeSpan> resultSink) {
			this.resultSink = resultSink;
			underlyingTimer = Stopwatch.StartNew();
		}

		public void Dispose() {
			underlyingTimer.Stop();
			resultSink(underlyingTimer.Elapsed);
		}
	}
}
