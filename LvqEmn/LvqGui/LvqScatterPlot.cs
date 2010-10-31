using System;
using System.Collections.Generic;
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
using System.Windows.Controls;
using System.Threading.Tasks;


namespace LvqGui {
	public sealed class LvqScatterPlot : IDisposable {
		readonly UpdateSync updateSync = new UpdateSync();
		readonly object plotsSync = new object();
		SubPlots subplots;

		readonly Dispatcher lvqPlotDispatcher;

		int subModelIdx;

		public void DisplayModel(LvqDatasetCli dataset, LvqModelCli model, int new_subModelIdx) {
			lvqPlotDispatcher.BeginInvoke(() => {
				SubPlots newsubplots;

				if (dataset == null || model == null) {
					subPlotWindow.Title = "No Model Selected";
					newsubplots = null;
				} else {
					subPlotWindow.Title = model.ModelLabel;
					bool modelChange = subplots == null || subplots.dataset != dataset || subplots.model != model;
					newsubplots = modelChange ? new SubPlots(dataset, model) : subplots;
				}

				if (newsubplots == subplots && subModelIdx == new_subModelIdx)
					return;

				lock (plotsSync) {
					subplots = newsubplots;
					subModelIdx = new_subModelIdx;
				}

				QueueUpdate();
				RelayoutSubPlotWindow(true);
			});
		}

		private void RelayoutSubPlotWindow(bool resetChildrenFirst = false) {
			Grid plotGrid = (Grid)subPlotWindow.Content;
			if (subplots == null) {
				plotGrid.Children.Clear();
				return;
			}
			double ratio = subPlotWindow.ActualWidth / subPlotWindow.ActualHeight;

			int plotCount = subplots.plots.Length + 1;

			var layout = (
					from CellsWide in Enumerable.Range((int)Math.Sqrt(plotCount * ratio), 2)
					from CellsHigh in Enumerable.Range((int)Math.Sqrt(plotCount / ratio), 2)
					where CellsWide * CellsHigh >= plotCount
					orderby CellsWide * CellsHigh
					select new { CellsWide, CellsHigh }
				).First();

			var unitLength = new GridLength(1.0, GridUnitType.Star);
			while (plotGrid.ColumnDefinitions.Count < layout.CellsWide) plotGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = unitLength });
			if (plotGrid.ColumnDefinitions.Count > layout.CellsWide) plotGrid.ColumnDefinitions.RemoveRange(layout.CellsWide, plotGrid.ColumnDefinitions.Count - layout.CellsWide);

			while (plotGrid.RowDefinitions.Count < layout.CellsHigh) plotGrid.RowDefinitions.Add(new RowDefinition { Height = unitLength });
			if (plotGrid.RowDefinitions.Count > layout.CellsHigh) plotGrid.RowDefinitions.RemoveRange(layout.CellsHigh, plotGrid.RowDefinitions.Count - layout.CellsHigh);

			if (resetChildrenFirst) {
				plotGrid.Children.Clear();
				plotGrid.Children.Add(subplots.scatterPlot);
				foreach (var plot in subplots.plots) plotGrid.Children.Add(plot);
				foreach (PlotControl plot in plotGrid.Children) {
					plot.Margin = new Thickness(2.0);
					plot.Background = Brushes.White;

				}
			}

			for (int i = 0; i < subplots.plots.Length; ++i) {
				Grid.SetRow(subplots.plots[i], (i + 1) / layout.CellsWide);
				Grid.SetColumn(subplots.plots[i], (i + 1) % layout.CellsWide);
			}
		}
		Window subPlotWindow;

		public LvqScatterPlot() {
			lvqPlotDispatcher = WpfTools.StartNewDispatcher();
			lvqPlotDispatcher.BeginInvoke(MakeSubPlotWindow);
		}

		private void MakeSubPlotWindow() {
			double borderWidth = (SystemParameters.MaximizedPrimaryScreenWidth - SystemParameters.FullPrimaryScreenWidth) / 2.0;
			if (subPlotWindow != null && subPlotWindow.IsLoaded) return;
			subPlotWindow = new Window {
				Width = SystemParameters.FullPrimaryScreenWidth * 0.7,
				Height = SystemParameters.MaximizedPrimaryScreenHeight - borderWidth * 2,
				Title = "No Model Selected",
				Background = Brushes.Gray,
				Content = new Grid()
			};

			subPlotWindow.SizeChanged += (o, e) => RelayoutSubPlotWindow();
			subPlotWindow.Show();
			subPlotWindow.Top = 0;
			double subWindowLeft = subPlotWindow.Left = SystemParameters.FullPrimaryScreenWidth - subPlotWindow.Width;

			Application.Current.Dispatcher.BeginInvoke(() => {
				var mainWindow = Application.Current.MainWindow;
				if (mainWindow != null && mainWindow.Left + mainWindow.Width > subWindowLeft)
					mainWindow.Left = Math.Max(0, subWindowLeft - +mainWindow.Width);
			});
		}

		class SubPlots {
			public readonly LvqDatasetCli dataset;
			public readonly LvqModelCli model;
			public readonly IVizEngine<Point[]> prototypePositionsPlot;
			public readonly IVizEngine<Point[]>[] classPlots;
			public readonly IVizEngine<int> classBoundaries;
			public readonly PlotControl scatterPlot;
			public readonly IVizEngine<IEnumerable<LvqTrainingStatCli>>[] statPlots;
			public readonly PlotControl[] plots;

			public SubPlots(LvqDatasetCli dataset, LvqModelCli model) {
				this.dataset = dataset;
				this.model = model;
				if (model.IsProjectionModel) {
					prototypePositionsPlot = MakeProtoPositionGraph();
					classBoundaries = MakeClassBoundaryGraph();
					classPlots = MakePerClassScatterGraph(dataset);
					scatterPlot = MakeScatterPlotControl(model, classPlots.Select(viz => viz.Plot).Concat(new[] { prototypePositionsPlot.Plot, classBoundaries.Plot }));
				}

				plots = MakeDataPlots(dataset, model);//required
				statPlots = ExtractDataSinksFromPlots(plots);
			}

			static IVizEngine<IEnumerable<LvqTrainingStatCli>>[] ExtractDataSinksFromPlots(IEnumerable<PlotControl> plots) {
				return (
						from plot in plots
						from graph in plot.Graphs
						select (IVizEngine<IEnumerable<LvqTrainingStatCli>>)graph.Visualisation
					).ToArray();
			}

			static PlotControl[] MakeDataPlots(LvqDatasetCli dataset, LvqModelCli model) {
				return (
						from statname in model.TrainingStatNames.Select(TrainingStatName.Create)
						where statname.StatGroup != null
						group statname by statname.StatGroup into statGroup
						select new PlotControl {
							ShowGridLines = true,
							Title = statGroup.Key + ": " + model.ModelLabel,
							Tag = statGroup.Key,
							GraphsEnumerable = StatisticsPlotMaker.Create(statGroup.Key, statGroup, model.IsMultiModel, dataset.IsFolded()).ToArray(),
						}
					).ToArray();
			}

			static PlotControl MakeScatterPlotControl(LvqModelCli model, IEnumerable<IPlot> graphs) {
				return new PlotControl {
					ShowAxes = false,
					AttemptBorderTicks = false,
					ShowGridLines = false,
					Title = "ScatterPlot: " + model.ModelLabel,
					GraphsEnumerable = graphs
				};
			}

			static IVizEngine<Point[]>[] MakePerClassScatterGraph(LvqDatasetCli dataset) {
				return (
						from classColor in dataset.ClassColors
						select Plot.Create(new PlotMetaData { RenderColor = classColor, }, new VizPixelScatterSmart { CoverageRatio = 0.99, OverridePointCountEstimate = dataset.PointCount }).Visualisation
					).ToArray();
			}

			static IVizEngine<Point[]> MakeProtoPositionGraph() {
				return Plot.Create(new PlotMetaData { ZIndex = 1, }, new VizPixelScatterGeom { OverridePointCountEstimate = 30, CoverageRatio = 1.0 }).Visualisation;
			}

			IVizEngine<int> MakeClassBoundaryGraph() {
				return Plot.Create(new PlotMetaData { ZIndex = -1 }, new VizDelegateBitmap<int> { UpdateBitmapDelegate = UpdateClassBoundaries }).Visualisation;
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
		}

		void QueueUpdate() { ThreadPool.QueueUserWorkItem(o => UpdateQueueProcessor()); }
		void UpdateQueueProcessor() {
			while (updateSync.UpdateEnqueue_IsMyTurn()) {
				SubPlots currsubplots;

				int currSubModelIdx;
				lock (plotsSync) {
					currsubplots = subplots;
					currSubModelIdx = subModelIdx;
				}

				PerformDisplayUpdate(currsubplots, currSubModelIdx);
				if (updateSync.UpdateDone_IsQueueEmpty()) return;
			}
		}

		static void PerformDisplayUpdate(SubPlots subplots, int subModelIdx) {
			if (subplots == null) return;
			var currProjection = subplots.model.CurrentProjectionAndPrototypes(subModelIdx, subplots.dataset);
			DispatcherOperation scatterPlotOperation = null;
			if (currProjection.IsOk && subplots.prototypePositionsPlot != null) {
				Point[] prototypePositions = Points.ToMediaPoints(currProjection.Prototypes.Points);

				//var projectedPointsByLabel = Enumerable.Range(0, dataPoints.Length).ToLookup(i => currProjection.Data.ClassLabels[i], i =>  Points.GetPoint(currProjection.Data.Points, i));

				int[] pointCountPerClass = new int[subplots.dataset.ClassCount];
				foreach (int t in currProjection.Data.ClassLabels) pointCountPerClass[t]++;

				Point[][] projectedPointsByLabel = Enumerable.Range(0, subplots.dataset.ClassCount).Select(i => new Point[pointCountPerClass[i]]).ToArray();
				int[] pointIndexPerClass = new int[subplots.dataset.ClassCount];
				for (int i = 0; i < currProjection.Data.ClassLabels.Length; ++i) {
					int label = currProjection.Data.ClassLabels[i];
					projectedPointsByLabel[label][pointIndexPerClass[label]++] = Points.GetPoint(currProjection.Data.Points, i);
				}
				
				scatterPlotOperation = subplots.prototypePositionsPlot.Dispatcher.BeginInvoke((Action)(() => {
					subplots.prototypePositionsPlot.ChangeData(prototypePositions);
					subplots.classBoundaries.ChangeData(subModelIdx);
					for (int i = 0; i < subplots.classPlots.Length; ++i)
						subplots.classPlots[i].ChangeData(projectedPointsByLabel[i]);
				}), DispatcherPriority.Background);
			}

			var graphOperations = (
				from plot in subplots.statPlots
				group plot by plot.Dispatcher into plotgroup
				select plotgroup.Key.BeginInvokeBackground(() => { foreach (var sp in plotgroup) sp.ChangeData(subplots.model.TrainingStats); })
				).ToArray();

			foreach (var operation in graphOperations)
				operation.Wait();
			if (scatterPlotOperation != null)
				scatterPlotOperation.Wait();
		}

		public void Dispose() {
			lvqPlotDispatcher.Invoke(new Action(() => {
				subPlotWindow.Close();
				lvqPlotDispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
			}));
		}

		class TrainingStatName {
			public readonly string TrainingStatLabel, UnitLabel, StatGroup;
			public readonly int Index;

			TrainingStatName(string compoundName, int index) {
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
						(stat, color) => MakePlots(stat.TrainingStatLabel, stat.UnitLabel, color, stat.Index, isMultiModel)
					).SelectMany(s => s);
			}

			static readonly Color[] errorColors = new[] { Colors.Red, Color.FromRgb(0x8b, 0x8b, 0), };
			static readonly Color[] costColors = new[] { Colors.Blue, Colors.DarkCyan, };
			static IEnumerable<Color> ColorsForWindow(string windowTitle, int length) {
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
							info.values[statIdx] + (variant != 0 ? variant * info.stderror[statIdx] : 0)
						)
					).ToArray();
					return LimitSize(retval);
				};
			}

			static Point[] LimitSize(Point[] retval) {
				int scaleFac = retval.Length / 1000;
				if (scaleFac <= 1)
					return retval;
				Point[] newret = new Point[retval.Length / scaleFac];
				for (int i = 0; i < newret.Length; ++i) {
					for (int j = i * scaleFac; j < i * scaleFac + scaleFac; ++j) {
						newret[i] += new Vector(retval[j].X / scaleFac, retval[j].Y / scaleFac);
					}
				}
				return newret;
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


		internal static void QueueUpdateIfCurrent(LvqScatterPlot plotData, LvqDatasetCli dataset, LvqModelCli model) {
			if (plotData != null && plotData.subplots != null && plotData.subplots.dataset == dataset && plotData.subplots.model == model)
				plotData.QueueUpdate();
		}
	}
}
