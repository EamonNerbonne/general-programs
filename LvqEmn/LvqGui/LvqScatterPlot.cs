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

		public readonly LvqDataSetCli dataset;
		LvqModelCli currentModel;
		readonly Dispatcher dispatcher;

		bool busy, updateQueued;
		object syncroot = new object();
		public readonly TrainingControlValues trainingController;

		public LvqScatterPlot(LvqDataSetCli dataset, Dispatcher dispatcher, TrainingControlValues trainingController ) {
			this.trainingController = trainingController;
			this.dataset = dataset;
			this.dispatcher = dispatcher;
			prototypePositionsPlot = PlotData.Create(default(Point[]));
			prototypePositionsPlot.Visualizer = new VizPixelScatterGeom { OverridePointCountEstimate = 50, };
			classBoundaries = PlotData.Create(default(object), UpdateClassBoundaries);
			classPlots = Enumerable.Range(0, dataset.ClassCount).Select(i => {
				var graphplot = PlotData.Create(default(Point[]));
				graphplot.RenderColor = dataset.ClassColors[i];
				return graphplot;
			}).ToArray();
		}

		public LvqModelCli LvqModel { set { this.currentModel = value; QueueUpdate(); } get { return currentModel; } }

		public IEnumerable<IPlotWithSettings> Plots {
			get {
				yield return classBoundaries;
				foreach (var plot in classPlots) yield return plot;
				yield return prototypePositionsPlot;
			}
		}

		private void UpdateDisplay() {
			if (currentModel == null) return;

			lock (syncroot)
				if (busy) {
					updateQueued = true;
					return;
				} else {
					busy = true;
					updateQueued = false;
				}

			double[,] currPoints = currentModel.CurrentProjectionOf(dataset);
			if (currPoints == null) return;//model not initialized

			int[] labels = dataset.ClassLabels();

			Dictionary<int, Point[]> projectedPointsByLabel =
				Points.ToMediaPoints(currPoints)
				.Zip(labels, (point, label) => new { Point = point, Label = label })
				.GroupBy(labelledPoint => labelledPoint.Label, labelledPoint => labelledPoint.Point)
				.ToDictionary(group => group.Key, group => group.ToArray());

			Point[] prototypePositions = Points.ToMediaPoints(currentModel.PrototypePositions().Item1).ToArray();

			dispatcher.BeginInvoke((Action)(() => {
				foreach (var pointGroup in projectedPointsByLabel)
					classPlots[pointGroup.Key].Data = pointGroup.Value;
				prototypePositionsPlot.Data = prototypePositions;
				classBoundaries.TriggerDataChanged();
				bool isIdle = false;
				lock (syncroot) {
					busy = false;
					if (updateQueued)
						QueueUpdate();
					else
						isIdle = true;
				}
				if (isIdle)
					trainingController.OnIdle();
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
			var curModel = currentModel;

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
