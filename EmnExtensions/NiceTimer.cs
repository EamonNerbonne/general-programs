using System;
using System.IO;

namespace EmnExtensions
{
	public class NiceTimer
	{
		DateTime dt;
		string oldmsg;
		public double TimeMark(string msg) {
			double sec = ((TimeSpan)(DateTime.Now - dt)).TotalSeconds;
            if (Writer != null) {
                if (oldmsg != null) 
                    Writer.WriteLine(oldmsg + " TOOK " + sec + " secs.");
                Writer.WriteLine("MB alloc'd: {0}", System.GC.GetTotalMemory(false) >> 20);
                if (msg != null)
                    Writer.WriteLine("TIMING: " + msg);
            }
			dt = DateTime.Now;
			oldmsg = msg;
			return sec;
		}

		public TimeSpan ElapsedSinceMark { get { return DateTime.Now - dt; } }
        public TextWriter Writer {get;set;}
        public string TimingAction { get { return oldmsg; } set { TimeMark(value); } }

        /// <summary>
        /// Initializes a NiceTimer with the default output going to Console.Out
        /// </summary>
        public NiceTimer() : this(Console.Out) { }

        /// <summary>
        /// Initialize a nicetimer sending timing logging info to the provided logger.
        /// </summary>
        /// <param name="writer">The TextWriter to log timing info to, or null to disable logging.</param>
		public NiceTimer(TextWriter writer) {
            writer =writer ?? Console.Out;
			dt = default(DateTime);
			oldmsg = null;
            Writer = writer;
		}


        /// <summary>
        /// Times a particular action for a number of runs.  
        /// Low overhead.
        /// </summary>
		public double TimeAction(string actionName, int testCount, Action testRun) {
            TimeMark(null);
            if(Writer!=null)
			    Writer.Write("Timing "+testCount+" runs of "+ actionName+":");
			DateTime start = DateTime.Now;
			for(int i = 0; i < testCount; i++) testRun();
			DateTime end = DateTime.Now;
			double elapsedPerTest = (end-start).TotalSeconds / (double)testCount;
            if (Writer != null) 
                Writer.WriteLine(" " + elapsedPerTest + " sec each.");
			return elapsedPerTest;
		}
        /// <summary>
        /// Times a particular action for (at least) a certain length of time.  
        /// Slightly higher overhead due to timer use in the inner loop.
        /// </summary>
        public double TimeAction(string actionName, TimeSpan testDuration, Action testRun) {
            TimeMark(null);
            if (Writer != null)
                Writer.Write("Timing " + actionName + " for "+testDuration+":");
            int testCount=0;
            DateTime proposedEnd = DateTime.Now + testDuration;

            DateTime start = DateTime.Now;
            while (DateTime.Now < proposedEnd) { testRun(); testCount++; }
            DateTime end = DateTime.Now;
            double elapsedPerTest = (end - start).TotalSeconds / (double)testCount;
            if (Writer != null)
                Writer.WriteLine(" " + elapsedPerTest + " sec each.");
            return elapsedPerTest;
        }

        public static void Time(string actionName, Action toPerform) {
            new NiceTimer().TimeAction(actionName, toPerform);
        }
        public static T Time<T>(string actionName, Func<T> toPerform) {
            T t=default(T);
            new NiceTimer().TimeAction(actionName, () => { t = toPerform(); });
            return t;
        }



        public void TimeAction(string actionName, Action toPerform) {
            TimeMark(actionName);
            toPerform();
            TimeMark(null);
        }

        public void Done() {
            TimeMark(null);
        }
    }
}
