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

		public void DisplayModel(LvqDatasetCli dataset, LvqModels model, int new_subModelIdx, bool showSelectedModelGraphs, bool showBoundaries, bool showPrototypes) {
			lvqPlotDispatcher.BeginInvoke(() => {
				SubPlots newsubplots;

				if (dataset == null || model == null) {
					subPlotWindow.Title = "No Model Selected";
					newsubplots = null;
				} else {
					MakeSubPlotWindow();
					subPlotWindow.Title = model.ModelLabel;
					newsubplots = model.Tag as SubPlots;

					bool modelChange = newsubplots == null || newsubplots.dataset != dataset || newsubplots.model != model || newsubplots.ShowSelectedModelGraphs != showSelectedModelGraphs;
					if (modelChange)
						model.Tag = newsubplots = new SubPlots(dataset, model, showSelectedModelGraphs);
				}

				if (newsubplots == subplots && model.SelectedSubModel == new_subModelIdx)
					return;

				lock (plotsSync) {
					subplots = newsubplots;
					if (model != null)
						model.SelectedSubModel = new_subModelIdx;
				}
				ShowBoundaries(showBoundaries);
				ShowPrototypes(showPrototypes);
				RelayoutSubPlotWindow(true);
			});
		}

		public void ShowBoundaries(bool visible) {

			lock (plotsSync)
				if (subplots != null && subplots.classBoundaries != null)
					lvqPlotDispatcher.BeginInvoke(() =>
						subplots.classBoundaries.Plot.MetaData.Hidden = !visible
					);
			QueueUpdate();
		}

		public void ShowPrototypes(bool visible) {
			lock (plotsSync)
				if (subplots != null && subplots.classBoundaries != null)
					lvqPlotDispatcher.BeginInvoke(() => {
						foreach (var protoPlot in subplots.prototypeClouds)
							protoPlot.Plot.MetaData.Hidden = !visible;
					});
			QueueUpdate();
		}


		void RelayoutSubPlotWindow(bool resetChildrenFirst = false) {
			Grid plotGrid = (Grid)subPlotWindow.Content;
			if (subplots == null) {
				plotGrid.Children.Clear();
				return;
			}
			double ratio = subPlotWindow.ActualWidth / subPlotWindow.ActualHeight;


			if (resetChildrenFirst) {
				plotGrid.Children.Clear();
				if (subplots.scatterPlotControl != null)
					plotGrid.Children.Add(subplots.scatterPlotControl);
				foreach (var plot in subplots.plots) plotGrid.Children.Add(plot);
				foreach (PlotControl plot in plotGrid.Children) {
					plot.Margin = new Thickness(2.0);
					plot.Background = Brushes.White;
				}
			}

			int plotCount = plotGrid.Children.Count;

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


			for (int i = 0; i < plotGrid.Children.Count; ++i) {
				Grid.SetRow(plotGrid.Children[i], i / layout.CellsWide);
				Grid.SetColumn(plotGrid.Children[i], i % layout.CellsWide);
			}
		}
		Window subPlotWindow;
		CancellationToken exitToken;

		public LvqScatterPlot(CancellationToken exitToken) {
			this.exitToken = exitToken;

			lvqPlotDispatcher = WpfTools.StartNewDispatcher(ThreadPriority.BelowNormal);
			lvqPlotDispatcher.BeginInvoke(MakeSubPlotWindow);
			exitToken.Register(() => lvqPlotDispatcher.InvokeShutdown());
		}

		void MakeSubPlotWindow() {
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
			public readonly bool ShowSelectedModelGraphs;
			public readonly LvqDatasetCli dataset;
			public readonly LvqModels model;
			public readonly IVizEngine<LvqModels.ModelProjectionAndImage>[] prototypeClouds, dataClouds;
			public readonly IVizEngine<LvqModels.ModelProjectionAndImage> classBoundaries;
			public readonly PlotControl scatterPlotControl;
			public readonly IVizEngine<LvqModels>[] statPlots;
			public readonly PlotControl[] plots;

			public SubPlots(LvqDatasetCli dataset, LvqModels model, bool showSelectedModelGraphs) {
				ShowSelectedModelGraphs = showSelectedModelGraphs;
				this.dataset = dataset;
				this.model = model;
				if (model.IsProjectionModel) {
					prototypeClouds = MakePerClassScatterGraph(dataset, 0.3f, dataset.ClassCount * Math.Min(model.SubModels.First().PrototypeLabels.Length, 3), 1)
						.Select((graph, i) => graph.Map((LvqModels.ModelProjectionAndImage proj) => proj.PrototypesByLabel[i])).ToArray();
					classBoundaries = MakeClassBoundaryGraph();
					dataClouds = MakePerClassScatterGraph(dataset, 1.0f)
						.Select((graph, i) => graph.Map((LvqModels.ModelProjectionAndImage proj) => proj.PointsByLabel[i])).ToArray();
					scatterPlotControl = MakeScatterPlotControl(dataClouds.Concat(prototypeClouds).Select(viz => viz.Plot).Concat(new[] { classBoundaries.Plot }));
				}

				plots = MakeDataPlots(dataset, model, showSelectedModelGraphs);//required
				statPlots = ExtractDataSinksFromPlots(plots);
			}

			public void SetScatterBounds(Rect bounds) {
				foreach (IPlotMetaDataWriteable metadata in dataClouds.Concat(prototypeClouds).Select(viz => viz.Plot.MetaData).Concat(new[] { classBoundaries.Plot.MetaData })) {
					metadata.OverrideBounds = bounds;
				}
			}

			static IVizEngine<LvqModels>[] ExtractDataSinksFromPlots(IEnumerable<PlotControl> plots) {
				return (
						from plot in plots
						from graph in plot.Graphs
						select (IVizEngine<LvqModels>)graph.Visualisation
					).ToArray();
			}

			static PlotControl[] MakeDataPlots(LvqDatasetCli dataset, LvqModels model, bool showSelGraphs) {
				return (
						from statname in model.TrainingStatNames.Select(TrainingStatName.Create)
						where statname.StatGroup != null
						group statname by statname.StatGroup into statGroup
						select new PlotControl {
							ShowGridLines = true,
							//Title = statGroup.Key + ": " + model.ModelLabel,
							Tag = statGroup.Key,
							GraphsEnumerable = StatisticsPlotMaker.Create(statGroup.Key, statGroup, model.IsMultiModel, dataset.IsFolded() || dataset.HasTestSet(), showSelGraphs).ToArray(),
						}
					).ToArray();
			}

			static PlotControl MakeScatterPlotControl(IEnumerable<IPlot> graphs) {
				return new PlotControl {
					ShowAxes = false,
					AttemptBorderTicks = false,
					//ShowGridLines = true,
					UniformScaling = true,
					//Title = "ScatterPlot: " + model.ModelLabel,
					GraphsEnumerable = graphs
				};
			}

			static IVizEngine<Point[]>[] MakePerClassScatterGraph(LvqDatasetCli dataset, float colorIntensity, int? PointCount = null, int? zIndex = null) {
				return (
						from classColor in dataset.ClassColors
						let darkColor = Color.FromScRgb(1.0f, classColor.ScR * colorIntensity, classColor.ScG * colorIntensity, classColor.ScB * colorIntensity)
						select Plot.Create(
							new PlotMetaData { RenderColor = darkColor, ZIndex = zIndex ?? 0 },
							new VizPixelScatterSmart { CoverageRatio = 0.98, OverridePointCountEstimate = PointCount ?? dataset.PointCount, CoverageGradient = 5.0 }).Visualisation
					).ToArray();
			}

			IVizEngine<LvqModels.ModelProjectionAndImage> MakeClassBoundaryGraph() {
				return Plot.Create(new PlotMetaData { ZIndex = -1 }, new VizDelegateBitmap<LvqModels.ModelProjectionAndImage> { UpdateBitmapDelegate = UpdateClassBoundaries }).Visualisation;
			}

			Tuple<int, int> lastWidthHeight;
			public Tuple<int, int> LastWidthHeight { get { return lastWidthHeight; } }

			void UpdateClassBoundaries(WriteableBitmap bmp, Matrix dataToBmp, int width, int height, LvqModels.ModelProjectionAndImage lastProjection) {
				lastWidthHeight = Tuple.Create(width, height);
				bool hideBoundaries = classBoundaries.Plot.MetaData.Hidden;

				if (!hideBoundaries) {
					if (width != lastProjection.Width || height != lastProjection.Height || lastProjection.ImageData == null) {
						lastProjection = lastProjection.forModels.CurrentProjectionAndImage(lastProjection.forDataset, width, height, hideBoundaries);
						SetScatterBounds(lastProjection.Bounds);
					}
					bmp.WritePixels(new Int32Rect(0, 0, width, height), lastProjection.ImageData, width * 4, 0);
				}
			}
		}

		void QueueUpdate() { ThreadPool.QueueUserWorkItem(UpdateQueueProcessor); }
		void UpdateQueueProcessor(object _) {
			if (exitToken.IsCancellationRequested || !updateSync.UpdateEnqueue_IsMyTurn())
				return;
			SubPlots currsubplots;

			lock (plotsSync) {
				currsubplots = subplots;
			}

			Task displayUpdateTask =
				DisplayUpdateOperations(currsubplots)
				.Aggregate(default(Task),
					(current, currentOp) => current == null
						? Task.Factory.StartNew((Action)(() => currentOp.Wait()))
						: current.ContinueWith(task => currentOp.Wait())
				);

			if (displayUpdateTask == null) { if (!updateSync.UpdateDone_IsQueueEmpty()) QueueUpdate(); } else displayUpdateTask.ContinueWith(task => { if (!updateSync.UpdateDone_IsQueueEmpty()) QueueUpdate(); });
		}

		static IEnumerable<DispatcherOperation> DisplayUpdateOperations(SubPlots subplots) {
			if (subplots != null) {
				var wh = subplots.LastWidthHeight;
				var projectionAndImage = subplots.model.CurrentProjectionAndImage(subplots.dataset, wh == null ? 0 : wh.Item1, wh == null ? 0 : wh.Item2, subplots.classBoundaries !=null && subplots.classBoundaries.Plot.MetaData.Hidden);

				if (projectionAndImage != null && subplots.prototypeClouds != null)
					yield return subplots.scatterPlotControl.Dispatcher.BeginInvokeBackground(
						() => {
							subplots.SetScatterBounds(projectionAndImage.Bounds);
							subplots.classBoundaries.ChangeData(projectionAndImage);
							for (int i = 0; i < subplots.dataClouds.Length; ++i) {
								subplots.dataClouds[i].ChangeData(projectionAndImage);
								subplots.prototypeClouds[i].ChangeData(projectionAndImage);
							}
						});

				var graphOperationsLazy =
					from plot in subplots.statPlots
					group plot by plot.Dispatcher into plotgroup
					select plotgroup.Key.BeginInvokeBackground(() => { foreach (var sp in plotgroup) sp.ChangeData(subplots.model); });

				foreach (var op in graphOperationsLazy) yield return op;
			}
		}

		public void Dispose() {
			lvqPlotDispatcher.Invoke(new Action(() => {
				subPlotWindow.Close();
				lvqPlotDispatcher.InvokeShutdown();
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
			public static IEnumerable<PlotWithViz<LvqModels>> Create(string windowTitle, IEnumerable<TrainingStatName> stats, bool isMultiModel, bool hasTestSet, bool showSelGraphs) {
				var relevantStatistics = (hasTestSet ? stats : stats.Where(stat => !stat.TrainingStatLabel.StartsWith("Test"))).ToArray();

				return
					relevantStatistics.Zip(ColorsForWindow(windowTitle, relevantStatistics.Length),
						(stat, color) => MakePlots(stat.TrainingStatLabel, stat.UnitLabel, color, stat.Index, isMultiModel, isMultiModel && showSelGraphs)
					).SelectMany(s => s);
			}

			static readonly Color[] errorColors = new[] { Colors.Red, Color.FromRgb(0x8b, 0x8b, 0), };
			static readonly Color[] costColors = new[] { Colors.Blue, Colors.DarkCyan, };
			static IEnumerable<Color> ColorsForWindow(string windowTitle, int length) {
				return
					windowTitle == "Error Rates" ? errorColors :
					windowTitle == "Cost Function" ? costColors :
					WpfTools.MakeDistributedColors(length, new MersenneTwister(42));
			}
			static IEnumerable<PlotWithViz<LvqModels>> MakePlots(string dataLabel, string yunitLabel, Color color, int statIdx, bool doVariants, bool showSelGraph) {
				if (doVariants)
					yield return MakeRangePlot(null, yunitLabel, color, statIdx);
				if (showSelGraph)
					yield return MakeCurrPlot(null, yunitLabel, color, statIdx);


				yield return MakePlot(dataLabel, yunitLabel, color, statIdx);
			}


			static Func<LvqModels, Point[]> ModelToPointsMapper(int statIdx) {
				return model => LimitSize(model.TrainingStats.Where(info => info.Value[statIdx].IsFinite()).Select(info => new Point(info.Value[LvqTrainingStatCli.TrainingIterationI], info.Value[statIdx])).ToArray());
			}
			static Func<LvqModels, Point[]> SelectedModelToPointsMapper(int statIdx) {
				return model => LimitSize(model.SelectedStats.Where(stat => stat.values[statIdx].IsFinite()).Select(stat => new Point(stat.values[LvqTrainingStatCli.TrainingIterationI], stat.values[statIdx])).ToArray());
			}
			static Func<LvqModels, Tuple<Point[], Point[]>> ModelToRangeMapper(int statIdx) {
				return model => {
					var okstats = model.TrainingStats.Where(info => (info.Value[statIdx] + info.StandardError[statIdx]).IsFinite());
					return
					Tuple.Create(
						LimitSize(
							okstats.Select(info =>
								new Point(info.Value[LvqTrainingStatCli.TrainingIterationI], info.Value[statIdx] + info.StandardError[statIdx])
							).ToArray()),
						LimitSize(
							okstats.Select(info =>
								new Point(info.Value[LvqTrainingStatCli.TrainingIterationI], info.Value[statIdx] - info.StandardError[statIdx])
							).ToArray()
						)
					);
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

			static PlotWithViz<LvqModels> MakeCurrPlot(string dataLabel, string yunitLabel, Color color, int statIdx) {
				return Plot.Create(
					new PlotMetaData {
						DataLabel = dataLabel,
						RenderColor = color,
						XUnitLabel = "Training iterations",
						YUnitLabel = yunitLabel,
						AxisBindings = TickedAxisLocation.BelowGraph | TickedAxisLocation.LeftOfGraph,
						ZIndex = 1
					},
					new VizLineSegments {
						CoverageRatioY = 0.95,
						CoverageRatioGrad = 20.0,
						DashStyle = DashStyles.Dot,
					}.Map(SelectedModelToPointsMapper(statIdx)));
			}

			static PlotWithViz<LvqModels> MakePlot(string dataLabel, string yunitLabel, Color color, int statIdx) {
				return Plot.Create(
					new PlotMetaData {
						DataLabel = dataLabel,
						RenderColor = color,
						XUnitLabel = "Training iterations",
						YUnitLabel = yunitLabel,
						AxisBindings = TickedAxisLocation.BelowGraph | TickedAxisLocation.LeftOfGraph,
						ZIndex = 1
					},
					new VizLineSegments {
						CoverageRatioY = 0.95,
						CoverageRatioGrad = 20.0,
					}.Map(ModelToPointsMapper(statIdx)));
			}
			static PlotWithViz<LvqModels> MakeRangePlot(string dataLabel, string yunitLabel, Color color, int statIdx) {
				//Blend(color, Colors.White)
				color.ScA = 0.3f;
				return Plot.Create(
					new PlotMetaData {
						DataLabel = dataLabel,
						RenderColor = color,
						XUnitLabel = "Training iterations",
						YUnitLabel = yunitLabel,
						AxisBindings = TickedAxisLocation.BelowGraph | TickedAxisLocation.LeftOfGraph,
						ZIndex = 0
					},
					new VizDataRange {
						CoverageRatioY = 0.95,
						CoverageRatioGrad = 20.0,
					}.Map(ModelToRangeMapper(statIdx))
					);
			}
		}


		internal static void QueueUpdateIfCurrent(LvqScatterPlot plotData, LvqDatasetCli dataset, LvqModels model) {
			if (plotData != null && plotData.subplots != null && plotData.subplots.dataset == dataset && plotData.subplots.model == model)
				plotData.QueueUpdate();
		}
	}
}
