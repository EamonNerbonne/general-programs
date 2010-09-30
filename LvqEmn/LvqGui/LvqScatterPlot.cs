using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EmnExtensions.Wpf;
using EmnExtensions.Wpf.Plot;
using EmnExtensions.Wpf.Plot.VizEngines;
using LvqLibCli;
using System.ComponentModel;
using System.Diagnostics;
using EmnExtensions.MathHelpers;

namespace LvqGui {
	public class LvqScatterPlot {
		IPlotWriteable<Point[]> prototypePositionsPlot;
		IPlotWriteable<Point[]>[] classPlots;
		IPlotWriteable<int> classBoundaries;

		struct StatPlot {
			public IPlotWriteable<Point[]> plot;
			public Func<LvqTrainingStatCli, Point> extractor;

			public static Point PlainStat(LvqTrainingStatCli info, int statIdx) { return new Point(info.values[LvqTrainingStatCli.TrainingIterationI], info.values[statIdx]); }
			public static Point UpperStat(LvqTrainingStatCli info, int statIdx) { return new Point(info.values[LvqTrainingStatCli.TrainingIterationI], info.values[statIdx] + info.stderror[statIdx]); }
			public static Point LowerStat(LvqTrainingStatCli info, int statIdx) { return new Point(info.values[LvqTrainingStatCli.TrainingIterationI], info.values[statIdx] - info.stderror[statIdx]); }

			static StatPlot MakePlot(string dataLabel, string yunitLabel, bool isRight, Color color, int statIdx, int variant) {
				//variant 0 is plain, 1 is upper, -1 is lower.
				var statPlot = PlotData.Create(default(Point[]));
				statPlot.PlotClass = PlotClass.Line;
				statPlot.DataLabel = dataLabel;
				statPlot.RenderColor = color;
				statPlot.XUnitLabel = "Training iterations";
				statPlot.YUnitLabel = yunitLabel;
				if (variant == 0)
					statPlot.ZIndex = 1;
				//trainErr.MinimalBounds = new Rect(new Point(0, 0.001), new Point(0, 0));
				statPlot.AxisBindings = TickedAxisLocation.BelowGraph | (isRight ? TickedAxisLocation.RightOfGraph : TickedAxisLocation.LeftOfGraph);
				((IVizLineSegments)statPlot.Visualizer).CoverageRatioY = 0.95;
				((IVizLineSegments)statPlot.Visualizer).CoverageRatioGrad = 20.0;


				return new StatPlot {
					plot = statPlot,
					extractor =
						variant == 0 ? (info => PlainStat(info, statIdx)) :
						variant == 1 ? (info => UpperStat(info, statIdx)) :
						(Func<LvqTrainingStatCli, Point>)(info => LowerStat(info, statIdx))
				};
			}
			static Color Blend(Color a, Color b) {
				return Color.FromArgb((byte)(a.A + b.A + 1 >> 1), (byte)(a.R + b.R + 1 >> 1), (byte)(a.G + b.G + 1 >> 1), (byte)(a.B + b.B + 1 >> 1));
			}
			public static IEnumerable<StatPlot> MakePlots(string dataLabel, string yunitLabel, bool isRight, Color color, int statIdx, bool doVariants) {
				if (doVariants) {
					yield return MakePlot(null, yunitLabel, isRight, Blend(color, Colors.White), statIdx, 1);
					yield return MakePlot(null, yunitLabel, isRight, Blend(color, Colors.White), statIdx, -1);
				}
				yield return MakePlot(dataLabel, yunitLabel, isRight, color, statIdx, 0);
			}
		}

		StatPlot[] statPlots;

		public readonly LvqDatasetCli dataset;
		public readonly LvqModelCli model;
		readonly Dispatcher dispatcher;


		HashSet<Window> plotWindows = new HashSet<Window>();
		Window MakeSubWin(string title) {
			var win = new Window {
				Width = Application.Current.MainWindow.Width * 0.5,
				Height = Application.Current.MainWindow.Height * 0.8,
				Title = title,
				Content = new PlotControl() {
					ShowGridLines = true,
				}
			};
			win.Closing += PlotWindowClosing;
			plotWindows.Add(win);
			return win;
		}
		void PlotWindowClosing(object sender, CancelEventArgs e) {
			Window win = (Window)sender;
			plotWindows.Remove(win);
		}


		class TrainingStatName {
			public readonly string TrainingStatLabel, UnitLabel, StatGroup;
			public readonly int Index;
			public TrainingStatName(string compoundName, int index) {
				if (index < 0) throw new ArgumentException("index must be positive");
				Index = index;
				string[] splitName = compoundName.Split('|');
				if (splitName.Length < 2) throw new ArgumentException("compound name has too few components");
				if (splitName.Length > 3) throw new ArgumentException("compound name has too many components");
				TrainingStatLabel = splitName[0];
				UnitLabel = splitName[1];
				StatGroup = splitName.Length > 2 ? splitName[2] : null;
			}
			public static TrainingStatName Create(string compoundName, int index) { return new TrainingStatName(compoundName, index); }
		}

		bool busy, updateQueued;
		object syncroot = new object();
		public LvqScatterPlot(LvqDatasetCli dataset, LvqModelCli model, Dispatcher dispatcher, PlotControl scatterPlotControl,int selectedSubModel) {
			this.subModelIdx = selectedSubModel;
			this.dataset = dataset;
			this.model = model;
			this.dispatcher = dispatcher;

			MakeScatterPlots(scatterPlotControl);
			scatterPlotControl.Title = "ScatterPlot: " + model.ModelLabel;

			TrainingStatName[] statnames = model.TrainingStatNames.Select(TrainingStatName.Create).ToArray();
			TrainingStatName xAxis = statnames.Single(statname => statname.UnitLabel == "iterations");
			List<StatPlot> allplots = new List<StatPlot>();
			foreach (var statgroup in
				from statname in statnames
				where statname.StatGroup != null
				group statname by new { statname.UnitLabel, statname.StatGroup }) {
				var win = MakeSubWin(statgroup.Key.StatGroup);
				var plotControl = (PlotControl)win.Content;
				plotControl.Title = statgroup.Key.StatGroup + ": " + model.ModelLabel;
				var plotsForGroup = MakePlots(statgroup.Key.StatGroup, statgroup, model.IsMultiModel, dataset.IsFolded()).ToArray();
				foreach (var plot in plotsForGroup)
					plotControl.Graphs.Add(plot.plot);
				allplots.AddRange(plotsForGroup);
				win.Show();
			}
			statPlots = allplots.ToArray();
			QueueUpdate();
		}

		private void MakeScatterPlots(PlotControl plotControl) {
			prototypePositionsPlot = PlotData.Create(default(Point[]));
			prototypePositionsPlot.Visualizer = new VizPixelScatterGeom { OverridePointCountEstimate = 40, };
			classBoundaries = PlotData.Create(subModelIdx, UpdateClassBoundaries);
			classPlots = Enumerable.Range(0, dataset.ClassCount).Select(i => {
				var graphplot = PlotData.Create(default(Point[]));
				((IVizPixelScatter)graphplot.Visualizer).CoverageRatio = 0.999;
				graphplot.RenderColor = dataset.ClassColors[i];
				return graphplot;
			}).ToArray();

			plotControl.Graphs.Clear();
			foreach (var subplot in ScatterPlots)
				plotControl.Graphs.Add(subplot);
		}

		static Color[] errorColors = new[] { Colors.Red, Color.FromRgb(0x8b, 0x8b, 0), };
		static Color[] costColors = new[] { Colors.Blue, Colors.DarkCyan, };

		private static IEnumerable<StatPlot> MakePlots(string windowTitle, IEnumerable<TrainingStatName> stats, bool isMultiModel, bool hasTestSet) {
			var usedStats = (hasTestSet ? stats : stats.Where(stat => !stat.TrainingStatLabel.StartsWith("Training"))).ToArray();

			Color[] colors =
				windowTitle == "Error Rates" ? errorColors :
				windowTitle == "Cost Function" ? costColors :
				GraphRandomPen.MakeDistributedColors(usedStats.Length, new MersenneTwister(1 + windowTitle.GetHashCode()));
			return
			usedStats.Zip(colors, (stat, color) => StatPlot.MakePlots(stat.TrainingStatLabel, stat.UnitLabel, false, color, stat.Index, isMultiModel))
				.SelectMany(s => s);
		}

		public IEnumerable<IPlotWithSettings> ScatterPlots {
			get {
				yield return classBoundaries;
				foreach (var plot in classPlots) yield return plot;
				yield return prototypePositionsPlot;
			}
		}

		bool AcquireUpdateLock() {
			lock (syncroot)
				if (busy) { updateQueued = true; return false; } else { busy = true; updateQueued = false; return true; }
		}

		void ReleaseUpdateLock() {
			lock (syncroot) {
				busy = false;
				if (updateQueued)
					QueueUpdate();
			}
		}
		int subModelIdx=0;
		public int SubModelIndex {
			get { return subModelIdx; }
			set {
				if (value == subModelIdx) return;
				subModelIdx = value;
				QueueUpdate();
			}
		}

		private void UpdateDisplay() {
			if (model == null) return;

			if (!AcquireUpdateLock()) return;
			int currentSubModelIdx = subModelIdx;

			var trainingStats = model.TrainingStats;
			var statPlotData = Enumerable.Range(0, statPlots.Length).Select(si => trainingStats.Select(statPlots[si].extractor).ToArray()).ToArray();
			var currProjection = model.CurrentProjectionAndPrototypes(currentSubModelIdx,dataset);

			Point[] prototypePositions = !currProjection.IsOk ? default(Point[]) : Points.ToMediaPoints(currProjection.Prototypes.Points);
			Point[] points = Points.ToMediaPoints(currProjection.Data.Points);
			int[] pointCountPerClass = new int[classPlots.Length];
			if (currProjection.IsOk)
				for (int i = 0; i < currProjection.Data.ClassLabels.Length; ++i)
					pointCountPerClass[currProjection.Data.ClassLabels[i]]++;

			Point[][] projectedPointsByLabel = Enumerable.Range(0, classPlots.Length).Select(i => new Point[pointCountPerClass[i]]).ToArray();
			int[] pointIndexPerClass = new int[classPlots.Length];
			if (currProjection.IsOk)
				for (int i = 0; i < points.Length; ++i) {
					int label = currProjection.Data.ClassLabels[i];
					projectedPointsByLabel[label][pointIndexPerClass[label]++] = points[i];
				}

			dispatcher.BeginInvoke((Action)(() => {
				for (int i = 0; i < classPlots.Length; ++i)
					classPlots[i].Data = projectedPointsByLabel[i];
				prototypePositionsPlot.Data = prototypePositions;
				classBoundaries.Data = currentSubModelIdx;
				classBoundaries.TriggerDataChanged(); //even if same index, underlying model _has_ changed.

				for (int i = 0; i < statPlots.Length; i++)
					statPlots[i].plot.Data = statPlotData[i];
				ReleaseUpdateLock();
			}), DispatcherPriority.Background);
		}

		public void QueueUpdate() { ThreadPool.QueueUserWorkItem(o => { UpdateDisplay(); }); }


		void UpdateClassBoundaries(WriteableBitmap bmp, Matrix dataToBmp, int width, int height, int subModelIdx) {
#if DEBUG
			int renderwidth = (width + 7) / 8;
			int renderheight = (height + 7) / 8;
#else
			int renderwidth = width;
			int renderheight = height;
#endif
			var curModel = model;

			if (curModel == null || width < 1 || height < 1)
				return;
			Matrix bmpToData = dataToBmp;
			bmpToData.Invert();
			Point topLeft = bmpToData.Transform(new Point(0.0, 0.0));
			Point botRight = bmpToData.Transform(new Point(width, height));
			int[,] closestClass = curModel.ClassBoundaries(subModelIdx,topLeft.X, botRight.X, topLeft.Y, botRight.Y, renderwidth, renderheight);
			if (closestClass == null) //uninitialized
				return;
			uint[] nativeColor = dataset.ClassColors
				.Select(c => { c.ScA = 0.1f; return c; })
				.Concat(Enumerable.Repeat(Color.FromRgb(0, 0, 0), 1))
				.Select(c => c.ToNativeColor())
				.ToArray();

			var edges = new List<Tuple<int, int>>();
			for (int y = 1; y < closestClass.GetLength(0) - 1; y++)
				for (int x = 1; x < closestClass.GetLength(1) - 1; x++) {
					if (false
						|| closestClass[y, x] != closestClass[y + 1, x]
						|| closestClass[y, x] != closestClass[y, x + 1]
						|| closestClass[y, x] != closestClass[y, x - 1]
						|| closestClass[y, x] != closestClass[y - 1, x]
						)
						edges.Add(Tuple.Create(y, x));
				}
			foreach (var coord in edges)
				closestClass[coord.Item1, coord.Item2] = nativeColor.Length - 1;
			uint[] classboundaries = new uint[width * height];
			int px = 0;
			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
					classboundaries[px++] = nativeColor[closestClass[y * renderheight / height, x * renderwidth / width]];
			bmp.WritePixels(new Int32Rect(0, 0, width, height), classboundaries, width * 4, 0);
		}

		public void ClosePlots() {
			foreach (Window win in plotWindows.ToArray()) {
				win.Close();
			}
			Console.WriteLine("registered windows:" + plotWindows.Count);

		}
	}
}
