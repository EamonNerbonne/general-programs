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

		IPlotWriteable<Point[]> trainErr, trainCost, pNorm;
		IPlotWriteable<Point[]>[] otherStats;


		public readonly LvqDatasetCli dataset;
		public readonly LvqModelCli model;
		readonly Dispatcher dispatcher;

		bool busy, updateQueued;
		object syncroot = new object();
		public LvqScatterPlot(LvqDatasetCli dataset, LvqModelCli model, Dispatcher dispatcher, PlotControl scatterPlotControl, PlotControl trainingStatsControl, PlotControl trainingNormPlotControl) {
			this.dataset = dataset;
			this.model = model;
			this.dispatcher = dispatcher;

			MakeScatterPlots(scatterPlotControl);

			MakeTrainingStatsPlots(trainingStatsControl);
			MakeNormPlots(trainingNormPlotControl);

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


		private void MakeTrainingStatsPlots(PlotControl trainingStatsControl) {
			trainErr = PlotData.Create(default(Point[]));
			trainErr.PlotClass = PlotClass.Line;
			trainErr.DataLabel = "Training error-rate";
			trainErr.RenderColor = Colors.Red;
			trainErr.XUnitLabel = "Training iterations";
			trainErr.YUnitLabel = "Training error-rate";
			//trainErr.MinimalBounds = new Rect(new Point(0, 0.001), new Point(0, 0));
			trainErr.AxisBindings = TickedAxisLocation.BelowGraph | TickedAxisLocation.RightOfGraph;
			((IVizLineSegments)trainErr.Visualizer).CoverageRatioY = 0.95;
			((IVizLineSegments)trainErr.Visualizer).CoverageRatioGrad = 10.0;

			trainCost = PlotData.Create(default(Point[]));
			trainCost.PlotClass = PlotClass.Line;
			trainCost.DataLabel = "Training cost-function";
			trainCost.RenderColor = Colors.Blue;
			trainCost.XUnitLabel = "Training iterations";
			trainCost.YUnitLabel = "Training cost-function";
			((IVizLineSegments)trainCost.Visualizer).CoverageRatioY = 0.95;
			((IVizLineSegments)trainCost.Visualizer).CoverageRatioGrad = 5.0;

			trainingStatsControl.Graphs.Clear();
			trainingStatsControl.Graphs.Add(trainCost);
			trainingStatsControl.Graphs.Add(trainErr);

		}

		private void MakeNormPlots(PlotControl trainingNormPlotControl) {
			pNorm = PlotData.Create(default(Point[]));
			pNorm.PlotClass = PlotClass.Line;
			pNorm.DataLabel = "(mean) Projection norm";
			pNorm.XUnitLabel = "Training iterations";
			pNorm.YUnitLabel = "norm";
			pNorm.RenderColor = Colors.Green;

			otherStats = Enumerable.Range(0, model.OtherStatCount()).Select(i => {
				var extraPlot = PlotData.Create(default(Point[]));
				extraPlot.PlotClass = PlotClass.Line;
				extraPlot.DataLabel = "extra data";
				extraPlot.XUnitLabel = "Training iterations";
				extraPlot.YUnitLabel = "norm";
				extraPlot.RenderColor = Colors.LightGreen;
				return extraPlot;
			}).ToArray();
			
			trainingNormPlotControl.Graphs.Clear();
			foreach (var plot in otherStats) trainingNormPlotControl.Graphs.Add(plot);
			trainingNormPlotControl.Graphs.Add(pNorm);
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

				trainErr.Data = trainingStats.Select(stat => new Point(stat.trainingIter, stat.trainingError)).ToArray(); 
				trainCost.Data = trainingStats.Select(stat => new Point(stat.trainingIter, stat.trainingCost)).ToArray(); 
				pNorm.Data = trainingStats.Select(stat => new Point(stat.trainingIter, stat.pNorm)).ToArray();
				for (int i = 0; i < otherStats.Length; i++) 
					otherStats[i].Data = trainingStats.Select(stat => new Point(stat.trainingIter, stat.otherStats[i])).ToArray();
				
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
