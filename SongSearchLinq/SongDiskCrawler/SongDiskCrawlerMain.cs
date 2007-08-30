using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using TagLib;
using System.IO;
using SongDataLib;
using EamonExtensionsLinq;
using EamonExtensionsLinq.Filesystem;

namespace TagLibSharp_LINQ
{
	class Timer
	{
		DateTime start;
		DateTime prev;
		public Timer() {
			start = DateTime.Now;
			prev = start;
		}
		public TimeSpan Mark() {
			DateTime now = DateTime.Now;
			TimeSpan retval = now - prev;
			prev = now;
			return retval;
		}
		public TimeSpan TimeSinceStart() {
			return DateTime.Now - start;
		}
		public TimeSpan TimeSinceStartUntilMark() {
			return prev - start;
		}
	}


	class Program
	{

		static void Main(string[] args) {
			Timer timer=new Timer();
			Console.Write("Loading Config file " + args[0]+"...");
			DatabaseConfigFile dcf = new DatabaseConfigFile(new FileInfo(args[0]));
			Console.WriteLine("done in {0:f03}.",timer.Mark().TotalSeconds);
			
			Console.Write("Loading song databases...");
			dcf.Load();
			Console.WriteLine("done in {0:f03}.", timer.Mark().TotalSeconds);
			
			Console.Write("Rescanning local files...");
			dcf.Rescan();
			Console.WriteLine("done in {0:f03}.", timer.Mark().TotalSeconds);
			
			Console.Write("Saving...");
			dcf.Save();
			Console.WriteLine("done in {0:f03}.", timer.Mark().TotalSeconds);
		}
	}
}