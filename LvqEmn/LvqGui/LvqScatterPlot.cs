using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
using EmnExtensions;


namespace LvqGui {
	public sealed class LvqScatterPlot : IDisposable {
		readonly UpdateSync updateSync = new UpdateSync();
		readonly object plotsSync = new object();
		SubPlots subplots;

		readonly Dispatcher lvqPlotDispatcher;

		int subModelIdx;

		public void DisplayModel(LvqDatasetCli dataset, LvqModels model, int new_subModelIdx) {
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
		CancellationToken exitToken;
		public LvqScatterPlot(CancellationToken exitToken) {
			this.exitToken = exitToken;

			lvqPlotDispatcher = WpfTools.StartNewDispatcher();
			lvqPlotDispatcher.BeginInvoke(MakeSubPlotWindow);
			exitToken.Register(() => lvqPlotDispatcher.BeginInvokeShutdown(DispatcherPriority.Send));
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
			public readonly LvqModels model;
			public readonly IVizEngine<Point[]>[] prototypePositionsPlot,classPlots;
			public readonly IVizEngine<LvqModels.ModelProjectionAndImage> classBoundaries;
			public readonly PlotControl scatterPlot;
			public readonly IVizEngine<IEnumerable<LvqModels.Statistic>>[] statPlots;
			public readonly PlotControl[] plots;

			public SubPlots(LvqDatasetCli dataset, LvqModels model) {
				this.dataset = dataset;
				this.model = model;
				if (model.IsProjectionModel) {
					prototypePositionsPlot = MakePerClassScatterGraph(dataset, 0.5f, dataset.ClassCount * 5,1);
					classBoundaries = MakeClassBoundaryGraph();
					classPlots = MakePerClassScatterGraph(dataset, 1.0f);
					scatterPlot = MakeScatterPlotControl(model, classPlots.Concat(prototypePositionsPlot).Select(viz => viz.Plot).Concat(new[] { classBoundaries.Plot }));
				}

				plots = MakeDataPlots(dataset, model);//required
				statPlots = ExtractDataSinksFromPlots(plots);
			}

			public void SetScatterBounds(Rect bounds) {
				foreach (IPlotMetaDataWriteable metadata in classPlots.Concat(prototypePositionsPlot).Select(viz => viz.Plot.MetaData).Concat(new[] { classBoundaries.Plot.MetaData })) {
					metadata.OverrideBounds = bounds;
				}
			}

			static IVizEngine<IEnumerable<LvqModels.Statistic>>[] ExtractDataSinksFromPlots(IEnumerable<PlotControl> plots) {
				return (
						from plot in plots
						from graph in plot.Graphs
						select (IVizEngine<IEnumerable<LvqModels.Statistic>>)graph.Visualisation
					).ToArray();
			}

			static PlotControl[] MakeDataPlots(LvqDatasetCli dataset, LvqModels model) {
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

			static PlotControl MakeScatterPlotControl(LvqModels model, IEnumerable<IPlot> graphs) {
				return new PlotControl {
					ShowAxes = false,
					AttemptBorderTicks = false,
					ShowGridLines = false,
					Title = "ScatterPlot: " + model.ModelLabel,
					GraphsEnumerable = graphs
				};
			}

			static IVizEngine<Point[]>[] MakePerClassScatterGraph(LvqDatasetCli dataset, float colorIntensity, int? PointCount=null,int? zIndex=null) {
				return (
						from classColor in dataset.ClassColors
						let darkColor = Color.FromScRgb(1.0f, classColor.ScR * colorIntensity, classColor.ScG * colorIntensity, classColor.ScB * colorIntensity)
						select Plot.Create(new PlotMetaData { RenderColor = darkColor, ZIndex = zIndex??0 }, new VizPixelScatterSmart { CoverageRatio = 0.99, OverridePointCountEstimate = PointCount ?? dataset.PointCount }).Visualisation
					).ToArray();
			}

			static IVizEngine<Point[]> MakeProtoPositionGraph() {
				return Plot.Create(new PlotMetaData { ZIndex = 1, }, new VizPixelScatterGeom { OverridePointCountEstimate = 30, CoverageRatio = 1.0 }).Visualisation;
			}

			IVizEngine<LvqModels.ModelProjectionAndImage> MakeClassBoundaryGraph() {
				return Plot.Create(new PlotMetaData { ZIndex = -1 }, new VizDelegateBitmap<LvqModels.ModelProjectionAndImage> { UpdateBitmapDelegate = UpdateClassBoundaries }).Visualisation;
			}

			Tuple<int, int> lastWidthHeight;
			public Tuple<int, int> LastWidthHeight { get { return lastWidthHeight; } }

			void UpdateClassBoundaries(WriteableBitmap bmp, Matrix dataToBmp, int width, int height, LvqModels.ModelProjectionAndImage lastProjection) {
				lastWidthHeight = Tuple.Create(width, height);

				if (width != lastProjection.Width || height != lastProjection.Height)
					lastProjection = lastProjection.forModels.CurrentProjectionAndImage(lastProjection.forSubModel, lastProjection.forDataset, width, height);
				bmp.WritePixels(new Int32Rect(0, 0, width, height), lastProjection.ImageData, width * 4, 0);
			}
		}

		void QueueUpdate() { Task.Factory.StartNew(UpdateQueueProcessor, CancellationToken.None, TaskCreationOptions.None, LowPriorityTaskScheduler.DefaultLowPriorityScheduler); }
		void UpdateQueueProcessor() {
			if (exitToken.IsCancellationRequested || !updateSync.UpdateEnqueue_IsMyTurn())
				return;
			SubPlots currsubplots;

			int currSubModelIdx;
			lock (plotsSync) {
				currsubplots = subplots;
				currSubModelIdx = subModelIdx;
			}

			var inflightOps = PerformDisplayUpdate(currsubplots, currSubModelIdx);
			Task lastTask = null;
			for (int i = 0; i < inflightOps.Length; i++) {
				var currentOp = inflightOps[i];
				lastTask = lastTask == null ? Task.Factory.StartNew(() => currentOp.Wait()) : lastTask.ContinueWith(task => currentOp.Wait());
			}

			if (lastTask == null) { if (!updateSync.UpdateDone_IsQueueEmpty()) QueueUpdate(); } else lastTask.ContinueWith(task => { if (!updateSync.UpdateDone_IsQueueEmpty()) QueueUpdate(); });
		}

		static DispatcherOperation[] PerformDisplayUpdate(SubPlots subplots, int subModelIdx) {
			if (subplots == null) return new DispatcherOperation[] { };
			var wh = subplots.LastWidthHeight;
			var projectionAndImage = subplots.model.CurrentProjectionAndImage(subModelIdx, subplots.dataset, wh == null ? 0 : wh.Item1, wh == null ? 0 : wh.Item2);
			DispatcherOperation scatterPlotOperation = null;
			if (projectionAndImage != null && subplots.prototypePositionsPlot != null) {
				scatterPlotOperation = subplots.scatterPlot.Dispatcher.BeginInvoke((Action)(() => {
					subplots.SetScatterBounds(projectionAndImage.Bounds);
					subplots.classBoundaries.ChangeData(projectionAndImage);
					for (int i = 0; i < subplots.classPlots.Length; ++i) {
						subplots.classPlots[i].ChangeData(projectionAndImage.PointsByLabel[i]);
						subplots.prototypePositionsPlot[i].ChangeData(projectionAndImage.PrototypesByLabel[i]);
					}
				}), DispatcherPriority.Background);
			}

			var graphOperationsLazy =
				from plot in subplots.statPlots
				group plot by plot.Dispatcher into plotgroup
				select plotgroup.Key.BeginInvokeBackground(() => { foreach (var sp in plotgroup) sp.ChangeData(subplots.model.TrainingStats); });


			return graphOperationsLazy.Concat(scatterPlotOperation != null ? new[] { scatterPlotOperation } : new DispatcherOperation[] { }).ToArray();
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
			public static IEnumerable<PlotWithViz<IEnumerable<LvqModels.Statistic>>> Create(string windowTitle, IEnumerable<TrainingStatName> stats, bool isMultiModel, bool hasTestSet) {
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
			static IEnumerable<PlotWithViz<IEnumerable<LvqModels.Statistic>>> MakePlots(string dataLabel, string yunitLabel, Color color, int statIdx, bool doVariants) {
				if (doVariants) {
					yield return MakePlot(null, yunitLabel, Blend(color, Colors.White), statIdx, 1);
					yield return MakePlot(null, yunitLabel, Blend(color, Colors.White), statIdx, -1);
				}
				yield return MakePlot(dataLabel, yunitLabel, color, statIdx, 0);
			}
			static Func<IEnumerable<LvqModels.Statistic>, Point[]> StatisticsToPointsMapper(int statIdx, int variant) {
				return stats => {
					var retval = stats.Select(info =>
						new Point(info.Value[LvqTrainingStatCli.TrainingIterationI],
							info.Value[statIdx] + (variant != 0 ? variant * info.StandardError[statIdx] : 0)
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

			static PlotWithViz<IEnumerable<LvqModels.Statistic>> MakePlot(string dataLabel, string yunitLabel, Color color, int statIdx, int variant) {
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


		internal static void QueueUpdateIfCurrent(LvqScatterPlot plotData, LvqDatasetCli dataset, LvqModels model) {
			if (plotData != null && plotData.subplots != null && plotData.subplots.dataset == dataset && plotData.subplots.model == model)
				plotData.QueueUpdate();
		}
	}
}
