using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KNucleotideFastaBench {
	class Wrapper {
		static void Main(string[] args) {
			if (args.Length > 0)
				Console.SetIn(File.OpenText(args[0]));
			var p = Process.GetCurrentProcess();
			var t0 = p.TotalProcessorTime;
			var sw = Stopwatch.StartNew();
			Program.Main();
			var elapsed = sw.Elapsed.TotalSeconds;
			//var mainThread = sw.CpuMilliseconds();
			var t1 = p.TotalProcessorTime;
			var MB = 1 / 1024.0 / 1024.0;
			var gcMB = GC.GetTotalMemory(false) * MB;
			var workingSetMB = p.WorkingSet64 * MB;
			var pagedMB = p.PagedMemorySize64 * MB;
			var peakPagedMB = p.PeakPagedMemorySize64 * MB;
			var peakWorkingSetMB = p.PeakWorkingSet64 * MB;


			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine("    Elapsed: {0:f2}s", elapsed);
			Console.WriteLine("        CPU: {0:f2}s", (t1 - t0).TotalSeconds);
			Console.WriteLine("Concurrency: {0:f2}x", (t1 - t0).TotalSeconds / elapsed);
			Console.WriteLine("   Overhead: {0:f2}s", t0.TotalSeconds);
			Console.WriteLine("  GC Memory: {0:f2}MB", gcMB);
			Console.WriteLine(" Peak WorkS: {0:f2}MB", peakWorkingSetMB);
			Console.WriteLine("      WorkS: {0:f2}MB", workingSetMB);
			Console.WriteLine(" Peak Paged: {0:f2}MB", pagedMB);
			Console.WriteLine("      Paged: {0:f2}MB", peakPagedMB);
		}
	}
}
