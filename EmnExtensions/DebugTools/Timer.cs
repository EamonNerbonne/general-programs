using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.DebugTools
{
	public static class Timer
	{
		public static T Time<T>(this Func<T> function, Action<TimeSpan> resultsSink)
		{
			DateTime start = DateTime.Now;
			var result = function();
			DateTime end = DateTime.Now;
			resultsSink(end - start);
			return result;
		}

		public static T Time<T,M>(this Func<T> function, M key, Action<M,TimeSpan> resultsSink)
		{
			DateTime start = DateTime.Now;
			var result = function();
			DateTime end = DateTime.Now;
			resultsSink(key, end - start);
			return result;
		}
	}
}
