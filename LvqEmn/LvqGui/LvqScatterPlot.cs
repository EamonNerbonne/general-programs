using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf;
using EmnExtensions.Wpf.Plot;
using EmnExtensions.Wpf.Plot.VizEngines;
using LvqLibCli;


namespace LvqGui {
	public sealed class LvqScatterPlot : IDisposable {
		readonly double winSize;
		readonly UpdateSync updateSync = new UpdateSync();

		readonly Dispatcher lvqPlotDispatcher;
		readonly List<Window> plotWindows = new List<Window>();

		readonly LvqDatasetCli dataset;
		public LvqDatasetCli Dataset { get { return dataset; } }

		readonly LvqModelCli model;
		public LvqModelCli Model { get { return model; } }

		IVizEngine<Point[]> prototypePositionsPlot;
		IVizEngine<Point[]>[] classPlots;
		IVizEngine<int> classBoundaries;
		//accessed from multiple threads:
		IVizEngine<IEnumerable<LvqTrainingStatCli>>[] statPlots = new IVizEngine<IEnumerable<LvqTrainingStatCli>>[] { };

		public LvqScatterPlot(LvqDatasetCli dataset, LvqModelCli model, int selectedSubModel) {
			if (!updateSync.UpdateEnqueue_IsMyTurn()) throw new InvalidAsynchronousStateException("Update can't be claimed yet!");
			this.subModelIdx = selectedSubModel;
			this.dataset = dataset;
			this.model = model;
			this.winSize = Math.Sqrt(Application.Current.MainWindow.Width * Application.Current.MainWindow.Height * 0.5);
			this.lvqPlotDispatcher = WpfTools.StartNewDispatcher();

			lvqPlotDispatcher.BeginInvoke(() => {
				OpenSubWindows();
				updateSync.UpdateDone_IsQueueEmpty();
				QueueUpdate();
			});
		}

		void OpenSubWindows() {
			ClosePlots();

			if (model.IsProjectionModel)
				plotWindows.Add(new Window { Width = winSize, Height = winSize, Title = "ScatterPlot", Content = MakeScatterPlots() });

			var plotGroups = (
					from statname in model.TrainingStatNames.Select(TrainingStatName.Create)
					where statname.StatGroup != null
					group statname by new { statname.UnitLabel, statname.StatGroup } into statGroup
					let winTitle = statGroup.Key.StatGroup
					let plots = StatisticsPlotMaker.Create(winTitle, statGroup, model.IsMultiModel, dataset.IsFolded()).ToArray()
					select new { WindowTitle = winTitle, Plots = plots, }
				).ToArray();

			plotWindows.AddRange(
				from plotGroup in plotGroups
				select new Window {
					Width = winSize,
					Height = winSize,
					Title = plotGroup.WindowTitle,
					Content = new PlotControl() {
						ShowGridLines = true,
						Title = plotGroup.WindowTitle + ": " + model.ModelLabel,
						GraphsEnumerable = plotGroup.Plots
					}
				}
			);

			statPlots = (
					from plotGroup in plotGroups
					from plot in plotGroup.Plots
					select plot.Visualisation
				).ToArray();

			foreach (var window in plotWindows.AsEnumerable(). Reverse()) window.Show();
		}

		PlotControl MakeScatterPlots() {
			prototypePositionsPlot = Plot.Create(new PlotMetaData { ZIndex = 1, }, new VizPixelScatterGeom { OverridePointCountEstimate = 30, }).Visualisation;
			classBoundaries = Plot.Create(new PlotMetaData { ZIndex = -1 }, new VizDelegateBitmap<int> { UpdateBitmapDelegate = UpdateClassBoundaries }).Visualisation;

			var classes = Enumerable.Range(0, dataset.ClassCount);
			classPlots = classes.Select(i =>
					Plot.Create(
						new PlotMetaData { RenderColor = dataset.ClassColors[i], },
						new VizPixelScatterSmart { CoverageRatio = 0.999 }
					).Visualisation
				).ToArray();

			return new PlotControl {
				ShowAxes = false,
				AttemptBorderTicks = false,
				ShowGridLines = false,
				Title = "ScatterPlot: " + model.ModelLabel,
				GraphsEnumerable = classPlots.Select(viz => viz.Plot).Concat(new IPlot[] { prototypePositionsPlot.Plot, classBoundaries.Plot })
			};
		}


		int subModelIdx = 0;
		public int SubModelIndex {
			get { return subModelIdx; }
			set {
				lvqPlotDispatcher.BeginInvoke(() => {
					if (value != subModelIdx) {
						subModelIdx = value;
						if (!plotWindows.Any() || !plotWindows.All(win => win.IsLoaded))
							OpenSubWindows();

						QueueUpdate();
					}
				});
			}
		}

		public void QueueUpdate() { ThreadPool.QueueUserWorkItem(o => { UpdateDisplay_BGThread(); }); }
		private void UpdateDisplay_BGThread() {
			if (model == null) return;
			while (updateSync.UpdateEnqueue_IsMyTurn()) {
				int currentSubModelIdx = subModelIdx;

				var currProjection = model.CurrentProjectionAndPrototypes(currentSubModelIdx, dataset);
				DispatcherOperation scatterPlotOperation = null;
				if (currProjection.IsOk) {
					Point[] prototypePositions = !currProjection.IsOk ? default(Point[]) : Points.ToMediaPoints(currProjection.Prototypes.Points);
					Point[] dataPoints = Points.ToMediaPoints(currProjection.Data.Points);
					//var dataIndices = Enumerable.Range(0, dataPoints.Length);
					//var projectedPointsByLabel = dataIndices.ToLookup(i => currProjection.Data.ClassLabels[i], i => dataPoints[i]);

					int[] pointCountPerClass = new int[dataset.ClassCount];
					for (int i = 0; i < currProjection.Data.ClassLabels.Length; ++i)
						pointCountPerClass[currProjection.Data.ClassLabels[i]]++;

					Point[][] projectedPointsByLabel = Enumerable.Range(0, dataset.ClassCount).Select(i => new Point[pointCountPerClass[i]]).ToArray();
					int[] pointIndexPerClass = new int[dataset.ClassCount];
					for (int i = 0; i < dataPoints.Length; ++i) {
						int label = currProjection.Data.ClassLabels[i];
						projectedPointsByLabel[label][pointIndexPerClass[label]++] = dataPoints[i];
					}

					scatterPlotOperation = prototypePositionsPlot.Dispatcher.BeginInvoke(() => {
						prototypePositionsPlot.ChangeData(prototypePositions);
						classBoundaries.ChangeData(currentSubModelIdx);
						for (int i = 0; i < classPlots.Length; ++i)
							classPlots[i].ChangeData(projectedPointsByLabel[i]);
					});
				}

				var graphOperations = statPlots.Select(plot => plot.BeginDataChange(model.TrainingStats)).ToArray();

				foreach (var operation in graphOperations)
					operation.Wait();
				if (currProjection.IsOk)
					scatterPlotOperation.Wait();

				if (updateSync.UpdateDone_IsQueueEmpty()) return;
			}
		}


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
			int[,] closestClass = curModel.ClassBoundaries(subModelIdx, topLeft.X, botRight.X, topLeft.Y, botRight.Y, renderwidth, renderheight);
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
					if (closestClass[y, x] != closestClass[y, x - 1]
						|| closestClass[y, x] != closestClass[y, x + 1]
						|| closestClass[y, x] != closestClass[y - 1, x]
						|| closestClass[y, x] != closestClass[y + 1, x]
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
			foreach (var win in plotWindows) win.Close();
			plotWindows.Clear();
		}

		public void Dispose() {
			lvqPlotDispatcher.Invoke(new Action(() => {
				ClosePlots();
				lvqPlotDispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
			}));
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


		static class StatisticsPlotMaker {
			public static IEnumerable<PlotWithViz<IEnumerable<LvqTrainingStatCli>>> Create(string windowTitle, IEnumerable<TrainingStatName> stats, bool isMultiModel, bool hasTestSet) {
				var relevantStatistics = (hasTestSet ? stats : stats.Where(stat => !stat.TrainingStatLabel.StartsWith("Training"))).ToArray();

				return
					relevantStatistics.Zip(ColorsForWindow(windowTitle, relevantStatistics.Length),
						(stat, color) => StatisticsPlotMaker.MakePlots(stat.TrainingStatLabel, stat.UnitLabel, color, stat.Index, isMultiModel)
					).SelectMany(s => s);
			}

			static Color[] errorColors = new[] { Colors.Red, Color.FromRgb(0x8b, 0x8b, 0), };
			static Color[] costColors = new[] { Colors.Blue, Colors.DarkCyan, };
			static Color[] ColorsForWindow(string windowTitle, int length) {
				return
					windowTitle == "Error Rates" ? errorColors :
					windowTitle == "Cost Function" ? costColors :
					WpfTools.MakeDistributedColors(length, new MersenneTwister(1 + windowTitle.GetHashCode()));
			}
			static IEnumerable<PlotWithViz<IEnumerable<LvqTrainingStatCli>>> MakePlots(string dataLabel, string yunitLabel, Color color, int statIdx, bool doVariants) {
				if (doVariants) {
					yield return MakePlot(null, yunitLabel, Blend(color, Colors.White), statIdx, 1);
					yield return MakePlot(null, yunitLabel, Blend(color, Colors.White), statIdx, -1);
				}
				yield return MakePlot(dataLabel, yunitLabel, color, statIdx, 0);
			}
			static Func<IEnumerable<LvqTrainingStatCli>, Point[]> StatisticsToPointsMapper(int statIdx, int variant) {
				return stats => {
					var retval = stats.Select(info =>
						new Point(info.values[LvqTrainingStatCli.TrainingIterationI],
							info.values[statIdx] + variant * info.stderror[statIdx]
						)
					).ToArray();
					int scaleFac = retval.Length / 2000;
					if (scaleFac <= 1)
						return retval;
					Point[] newret = new Point[retval.Length / scaleFac];
					for (int i = 0; i < newret.Length; ++i) {
						for (int j = i * scaleFac; j < i * scaleFac + scaleFac; ++j) {
							newret[i] += new Vector(retval[j].X / scaleFac, retval[j].Y / scaleFac);
						}
					}
					return newret;
				};
			}
			static PlotWithViz<IEnumerable<LvqTrainingStatCli>> MakePlot(string dataLabel, string yunitLabel, Color color, int statIdx, int variant) {
				return Plot.Create(
					new PlotMetaData {
						DataLabel = dataLabel,
						RenderColor = color,
						XUnitLabel = "Training iterations",
						YUnitLabel = yunitLabel,
						AxisBindings = TickedAxisLocation.BelowGraph | TickedAxisLocation.LeftOfGraph,
						ZIndex = variant == 0 ? 1 : 0
					},
					new VizLineSegments {
						CoverageRatioY = 0.95,
						CoverageRatioGrad = 20.0,
					}.Map(StatisticsToPointsMapper(statIdx, variant)));
			}
			static Color Blend(Color a, Color b) {
				return Color.FromArgb((byte)(a.A + b.A + 1 >> 1), (byte)(a.R + b.R + 1 >> 1), (byte)(a.G + b.G + 1 >> 1), (byte)(a.B + b.B + 1 >> 1));
			}
		}
	}
}
