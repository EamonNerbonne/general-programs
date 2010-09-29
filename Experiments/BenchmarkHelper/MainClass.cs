using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.Wpf.Plot;
using EmnExtensions.MathHelpers;
using EmnExtensions.DebugTools;
using EmnExtensions.Algorithms;
using System.Windows;

namespace BenchmarkHelper {
	public static class MainClass {

		internal static void MainRun(PlotControl plotControl) {
			MersenneTwister rnd = RndHelper.ThreadLocalRandom;
			double[] list0 = Enumerable.Repeat(0, MaxSize).Select(x => rnd.NextNormal()).ToArray();
			int kf = rnd.Next();
			double ignoreQ = 0, ignoreS = 0;
			var listQ = list0.ToArray();
			var listS = list0.ToArray();
			List<Point> quickselectTime =  new List<Point>(), slowselectTime= new List<Point>(); 
			foreach (int size in Sizes.Where(size=>size<2000)) {
				double durationS_ms = DTimer.BenchmarkAction(() => { ignoreS += SelectionAlgorithm.SlowSelect(listS, kf % size, 0, size); }, Math.Max( 10, MaxSize/size) ).TotalMilliseconds;
				double durationQ_ms = DTimer.BenchmarkAction(() => { ignoreQ += SelectionAlgorithm.QuickSelect(listQ, kf % size, 0, size); }, Math.Max(10, MaxSize / size)).TotalMilliseconds;
				quickselectTime.Add(new Point(size, durationQ_ms));
				slowselectTime.Add(new Point(size, durationS_ms));
			}
			plotControl.Dispatcher.BeginInvoke((Action)(() => {
				var qplot = PlotData.Create(quickselectTime.ToArray());
				qplot.PlotClass = PlotClass.Line;
				qplot.DataLabel = "QuickSelect";
				qplot.XUnitLabel = "array size";
				qplot.YUnitLabel = "QuickSelect time (ms)";
				var splot = PlotData.Create(slowselectTime.ToArray());
				splot.PlotClass = PlotClass.Line;
				splot.DataLabel = "SlowSelect";
				splot.XUnitLabel = "array size";
				splot.YUnitLabel = "SlowSelect time (ms)";
				splot.AxisBindings = TickedAxisLocation.RightOfGraph | TickedAxisLocation.BelowGraph;
				plotControl.Graphs.Add(qplot);
				plotControl.Graphs.Add(splot);
				plotControl.AutoPickColors();
			}));

		}

		const int MaxSize = 1000000;
		static IEnumerable<int> Sizes { get { for (int i = 1; i < MaxSize; i = (int)(i * 1.1) + 1) yield return i; } }

	}
}
