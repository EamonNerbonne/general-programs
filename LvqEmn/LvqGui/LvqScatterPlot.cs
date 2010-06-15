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

namespace LvqGui {
	public class LvqScatterPlot {
		IPlotWriteable<Point[]> prototypePositionsPlot;
		IPlotWriteable<Point[]>[] classPlots;
		IPlotWriteable<object> classBoundaries;

		struct StatPlot {
			public IPlotWriteable<Point[]> plot;
			public Func<LvqTrainingStatCli, Point> extractor;

			public static Point PlainStat(LvqTrainingStatCli info, int statIdx) { return new Point(info.trainingIter, info.values[statIdx]); }
			public static Point UpperStat(LvqTrainingStatCli info, int statIdx) { return new Point(info.trainingIter, info.values[statIdx] + info.stderror[statIdx]); }
			public static Point LowerStat(LvqTrainingStatCli info, int statIdx) { return new Point(info.trainingIter, info.values[statIdx] - info.stderror[statIdx]); }

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

		bool busy, updateQueued;
		object syncroot = new object();
		public LvqScatterPlot(LvqDatasetCli dataset, LvqModelCli model, Dispatcher dispatcher,
			PlotControl scatterPlotControl, PlotControl errorRatePlot, PlotControl costFuncPlot, PlotControl projectionNormPlot, PlotControl extraPlot
			) {
			this.dataset = dataset;
			this.model = model;
			this.dispatcher = dispatcher;

			MakeScatterPlots(scatterPlotControl);

			statPlots =
				new[]{
				MakeErrorRatePlots(errorRatePlot, model.IsMultiModel,dataset.IsFolded()),
				MakeCostPlots(costFuncPlot, model.IsMultiModel,dataset.IsFolded()),
				MakeNormPlots(projectionNormPlot, model.IsMultiModel),
				MakeExtraPlots(extraPlot, model.IsMultiModel, extraStatCount:model.OtherStatCount()),
				}.SelectMany(s => s).ToArray();

			Func<FrameworkElement, Window> getWin = dp => { while (!(dp is Window)) dp = (FrameworkElement)dp.Parent; return (Window)dp; };

			foreach (var plot in new[] { errorRatePlot, costFuncPlot, projectionNormPlot, extraPlot }) {
				plot.Title = getWin(plot).Title + ": " + model.ModelLabel;
			}
			scatterPlotControl.Title = "ScatterPlot: " + model.ModelLabel;
			QueueUpdate();
		}

		private void MakeScatterPlots(PlotControl plotControl) {
			prototypePositionsPlot = PlotData.Create(default(Point[]));
			prototypePositionsPlot.Visualizer = new VizPixelScatterGeom { OverridePointCountEstimate = 50, };
			classBoundaries = PlotData.Create(default(object), UpdateClassBoundaries);
			classPlots = Enumerable.Range(0, dataset.ClassCount).Select(i => {
				var graphplot = PlotData.Create(default(Point[]));
				((IVizPixelScatter)graphplot.Visualizer).CoverageRatio = 0.999;
				graphplot.RenderColor = dataset.ClassColors[i];
				return graphplot;
			}).ToArray();

			plotControl.Graphs.Clear();
			foreach (var subplot in Plots)
				plotControl.Graphs.Add(subplot);
		}

		private static IEnumerable<StatPlot> MakeErrorRatePlots(PlotControl plotControl, bool isMultiModel, bool hasTestSet) {
			plotControl.Graphs.Clear();
			foreach (StatPlot statGraph in
				new[]{
				StatPlot.MakePlots("Training error-rate", "error-rate", false, Colors.Red, LvqTrainingStatCli.TrainingErrorStat, isMultiModel),
				!hasTestSet?null: StatPlot.MakePlots("Test error-rate", "error-rate", false, Color.FromRgb(0x8b,0x8b,0), LvqTrainingStatCli.TestErrorStat, isMultiModel),
				}.Where(s => s != null).SelectMany(s => s)) {
				plotControl.Graphs.Add(statGraph.plot);
				yield return statGraph;
			}
		}

		private static IEnumerable<StatPlot> MakeCostPlots(PlotControl plotControl, bool isMultiModel, bool hasTestSet) {
			plotControl.Graphs.Clear();
			foreach (StatPlot plot in
				new[]{
				StatPlot.MakePlots("Training cost-function","cost-function", false, Colors.Blue, LvqTrainingStatCli.TrainingCostStat, isMultiModel),
				!hasTestSet?null: StatPlot.MakePlots("Test cost-function","cost-function", false, Colors.DarkCyan, LvqTrainingStatCli.TestCostStat, isMultiModel),
				}.Where(s => s != null).SelectMany(s => s)) {
				plotControl.Graphs.Add(plot.plot);
				yield return plot;
			}
		}

		private static IEnumerable<StatPlot> MakeNormPlots(PlotControl plotControl, bool isMultiModel) {
			plotControl.Graphs.Clear();
			foreach (StatPlot plot in
				StatPlot.MakePlots("(mean) Projection norm", "norm", false, Colors.Green, LvqTrainingStatCli.PNormStat, isMultiModel)
				) {
				plotControl.Graphs.Add(plot.plot);
				yield return plot;
			}
		}
		private static IEnumerable<StatPlot> MakeExtraPlots(PlotControl plotControl, bool isMultiModel, int extraStatCount) {
			plotControl.Graphs.Clear();

			Color[] cols = GraphRandomPen.MakeDistributedColors(extraStatCount+1);

			foreach (StatPlot plot in
					Enumerable.Range(0, extraStatCount).SelectMany(i =>
						StatPlot.MakePlots("extra data " + i, "extra data", false, cols[i], LvqTrainingStatCli.ExtraStat + i, isMultiModel))
				) {
				plotControl.Graphs.Add(plot.plot);
				yield return plot;
			}
		}


		public IEnumerable<IPlotWithSettings> Plots {
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

		private void UpdateDisplay() {
			if (model == null) return;

			if (!AcquireUpdateLock()) return;


			var trainingStats = model.TrainingStats;
			var statPlotData = Enumerable.Range(0, statPlots.Length).Select(si => trainingStats.Select(statPlots[si].extractor).ToArray()).ToArray();
			var currProjection = model.CurrentProjectionAndPrototypes(dataset);

			Dictionary<int, Point[]> projectedPointsByLabel =
				!currProjection.IsOk ? Enumerable.Range(0, classPlots.Length).ToDictionary(i => i, i => default(Point[])) :
				Points.ToMediaPoints(currProjection.Data.Points)
				.Zip(currProjection.Data.ClassLabels, (point, label) => new { Point = point, Label = label })
				.GroupBy(labelledPoint => labelledPoint.Label, labelledPoint => labelledPoint.Point)
				.ToDictionary(group => group.Key, group => group.ToArray());

			Point[] prototypePositions =
				!currProjection.IsOk ? default(Point[]) :
				Points.ToMediaPoints(currProjection.Prototypes.Points).ToArray();

			dispatcher.BeginInvoke((Action)(() => {
				foreach (var pointGroup in projectedPointsByLabel)
					classPlots[pointGroup.Key].Data = pointGroup.Value;
				prototypePositionsPlot.Data = prototypePositions;
				classBoundaries.TriggerDataChanged();

				for (int i = 0; i < statPlots.Length; i++)
					statPlots[i].plot.Data = statPlotData[i];
				ReleaseUpdateLock();
			}), DispatcherPriority.Background);
		}

		public void QueueUpdate() { ThreadPool.QueueUserWorkItem(o => { UpdateDisplay(); }); }


		void UpdateClassBoundaries(WriteableBitmap bmp, Matrix dataToBmp, int width, int height, object ignore) {
#if DEBUG
			int renderwidth = (width + 7) / 8;
			int renderheight = (height + 7) / 8;
#else
			int renderwidth = width;
			int renderheight = height;
#endif
			var curModel = model;

			if (curModel == null)
				return;
			Matrix bmpToData = dataToBmp;
			bmpToData.Invert();
			Point topLeft = bmpToData.Transform(new Point(0.0, 0.0));
			Point botRight = bmpToData.Transform(new Point(width, height));
			int[,] closestClass = curModel.ClassBoundaries(topLeft.X, botRight.X, topLeft.Y, botRight.Y, renderwidth, renderheight);
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
	}
}
