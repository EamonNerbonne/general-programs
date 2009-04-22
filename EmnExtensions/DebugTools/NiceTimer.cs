using System;
using System.IO;

namespace EmnExtensions.DebugTools
{

	public class NiceTimer
	{
		public struct TimingResults
		{
			public string ActionName;
			public double ElapsedSeconds;
			public override string ToString() { return (ActionName ?? "<unknown>") + " took " + ElapsedSeconds + "secs."; }
		}

		public struct TimingResultsAndValue<T>
		{
			public TimingResults Timing;
			public T Value;
		}

		DateTime dt;
		string oldmsg;
		public TimingResults TimeMark(string msg)
		{
			double sec = ((TimeSpan)(DateTime.Now - dt)).TotalSeconds;
			TimingResults retval = new TimingResults { ActionName = oldmsg, ElapsedSeconds = sec };
			if (Writer != null)
			{
				if (oldmsg != null)
					Writer.WriteLine(retval);
				Writer.WriteLine("MB alloc'd: {0}", System.GC.GetTotalMemory(false) >> 20);
				if (msg != null)
					Writer.WriteLine("TIMING: " + msg);
			}
			dt = DateTime.Now;
			oldmsg = msg;
			return retval;
		}

		public TimeSpan ElapsedSinceMark { get { return DateTime.Now - dt; } }
		public TextWriter Writer { get; set; }
		public string TimingAction { get { return oldmsg; } set { TimeMark(value); } }

		/// <summary>
		/// Initializes a NiceTimer with the default output going to Console.Out
		/// </summary>
		public NiceTimer() : this(Console.Out) { }

		/// <summary>
		/// Initialize a nicetimer sending timing logging info to the provided logger.
		/// </summary>
		/// <param name="writer">The TextWriter to log timing info to, or null to disable logging.</param>
		public NiceTimer(TextWriter writer)
		{
			writer = writer ?? Console.Out;
			dt = default(DateTime);
			//oldmsg = null;
			Writer = writer;
		}


		/// <summary>
		/// Times a particular action for a number of runs.  
		/// Low overhead.
		/// </summary>
		public TimingResults TimeAction(string actionName, int testCount, Action testRun)
		{
			TimeMark(null);
			if (Writer != null)
				Writer.Write("Timing " + testCount + " runs of " + actionName + ":");
			DateTime start = DateTime.Now;
			for (int i = 0; i < testCount; i++) testRun();
			DateTime end = DateTime.Now;
			double elapsedPerTest = (end - start).TotalSeconds / (double)testCount;
			if (Writer != null)
				Writer.WriteLine(" " + elapsedPerTest + " sec each.");
			return new TimingResults { ActionName = actionName, ElapsedSeconds = elapsedPerTest };
		}
		/// <summary>
		/// Times a particular action for (at least) a certain length of time.  
		/// Slightly higher overhead due to timer use in the inner loop.
		/// </summary>
		public TimingResults TimeAction(string actionName, TimeSpan testDuration, Action testRun)
		{
			TimeMark(null);
			if (Writer != null)
				Writer.Write("Timing " + actionName + " for " + testDuration + ":");
			int testCount = 0;
			DateTime proposedEnd = DateTime.Now + testDuration;

			DateTime start = DateTime.Now;
			while (DateTime.Now < proposedEnd) { testRun(); testCount++; }
			DateTime end = DateTime.Now;
			double elapsedPerTest = (end - start).TotalSeconds / (double)testCount;
			if (Writer != null)
				Writer.WriteLine(" " + elapsedPerTest + " sec each.");
			return new TimingResults { ActionName = actionName, ElapsedSeconds = elapsedPerTest };
		}

		public static TimingResults Time(string actionName, Action toPerform)
		{
			return new NiceTimer().TimeAction(actionName, toPerform);
		}

		public static TimingResultsAndValue<T> Time<T>(string actionName, Func<T> toPerform)
		{
			TimingResultsAndValue<T> retval = default(TimingResultsAndValue<T>);
			retval.Timing=new NiceTimer().TimeAction(actionName, () => { retval.Value = toPerform(); });
			return retval;
		}

		public TimingResults TimeAction(string actionName, Action toPerform)
		{
			TimeMark(actionName);
			toPerform();
			return TimeMark(null);
		}

		public TimingResults Done()
		{
			return TimeMark(null);
		}
	}
}
