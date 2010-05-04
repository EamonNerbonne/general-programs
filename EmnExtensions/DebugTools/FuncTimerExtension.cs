using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace EmnExtensions.DebugTools
{
	public static class FuncTimerExtension
	{
		public static T Time<T>(this Func<T> function, Action<TimeSpan> resultsSink)
		{
			Stopwatch timer = Stopwatch.StartNew();
			var result = function();
			timer.Stop();
			resultsSink(timer.Elapsed);
			return result;
		}

		public static T Time<T,M>(this Func<T> function, M key, Action<M,TimeSpan> resultsSink)
		{
			Stopwatch timer = Stopwatch.StartNew();
			var result = function();
			timer.Stop();
			resultsSink(key, timer.Elapsed);
			return result;
		}
	}
}
