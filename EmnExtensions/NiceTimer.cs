using System;

namespace EmnExtensions
{
	public class NiceTimer
	{
		DateTime dt;
		string oldmsg;
		public double TimeMark(string msg) {
			double sec = ((TimeSpan)(DateTime.Now - dt)).TotalSeconds;
			if(oldmsg != null) {
				Console.WriteLine(oldmsg + " TOOK " +sec + " secs.");
			}
			Console.WriteLine("MB alloc'd: {0}", System.GC.GetTotalMemory(false) >> 20);
			if(msg != null)
				Console.WriteLine("TIMING: " + msg);
			dt = DateTime.Now;
			oldmsg = msg;
			return sec;
		}

		public TimeSpan ElapsedSinceMark { get { return DateTime.Now - dt; } }

		public NiceTimer(string msg) {
			dt = default(DateTime);
			oldmsg = null;
			if(msg != null) TimeMark(msg);
		}

		static public double TimeAction(string actionName, int testCount, Action testRun) {
			Console.Write("Timing "+testCount+" runs of "+ actionName+":");
			DateTime start = DateTime.Now;
			for(int i = 0; i < testCount; i++) testRun();
			DateTime end = DateTime.Now;
			double elapsedPerTest = (end-start).TotalSeconds / (double)testCount;
			Console.WriteLine(" "+elapsedPerTest +" sec each.");
			return elapsedPerTest;
		}
	}
}
