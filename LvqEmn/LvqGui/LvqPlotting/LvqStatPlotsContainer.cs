using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using EmnExtensions.Wpf;
using EmnExtensions.Wpf.Plot;
using EmnExtensions.Wpf.Plot.VizEngines;
using LvqLibCli;
using EmnExtensions.Filesystem;
using System.IO;


namespace LvqGui {
	public sealed class LvqStatPlotsContainer : IDisposable {
		readonly object plotsSync = new object();
		LvqStatPlots subplots;

		readonly Dispatcher lvqPlotDispatcher;
		readonly TaskScheduler lvqPlotTaskScheduler;//corresponds to lvqPlotDispatcher


		public Task DisplayModel(LvqDatasetCli dataset, LvqMultiModel model, int new_subModelIdx, StatisticsViewMode viewMode, bool showBoundaries, bool showPrototypes, bool showTestEmbedding, bool showTestErrorRates) {
			if (lvqPlotDispatcher.HasShutdownStarted) throw new InvalidOperationException("Dispatcher shutting down");
			return lvqPlotTaskScheduler.StartNewTask(() => {
				lock (plotsSync) {
					if (dataset == null || model == null) {
						subPlotWindow.Title = "No Model Selected";
						subplots = null;
					} else {
						MakeSubPlotWindow();
						subPlotWindow.Title = model.ModelLabel;
						LvqStatPlots oldsubplots = model.Tag as LvqStatPlots;

						bool modelChange = oldsubplots == null || oldsubplots.dataset != dataset || oldsubplots.model != model || oldsubplots.plots.First().Dispatcher != lvqPlotDispatcher;
						if (modelChange)
							model.Tag = subplots = new LvqStatPlots(dataset, model);
						else
							subplots = oldsubplots;
						subplots.selectedSubModel = new_subModelIdx;
						subplots.showTestEmbedding = showTestEmbedding;
					}
					ShowBoundaries(showBoundaries);
					ShowCurrentProjectionStats(viewMode);
					ShowPrototypes(showPrototypes);
					RelayoutSubPlotWindow(true);
					ShowTestErrorRates(showTestErrorRates);
					QueueUpdate();
				}
			});
		}

		public void ShowBoundaries(bool visible) {
			lock (plotsSync)
				if (subplots != null && subplots.classBoundaries != null)
					lvqPlotDispatcher.BeginInvoke(() => {
						if (subplots != null && subplots.classBoundaries != null)
							subplots.classBoundaries.Plot.MetaData.Hidden = !visible;
					});
			QueueUpdate();
		}

		StatisticsViewMode currViewMode;
		public Task ShowCurrentProjectionStats(StatisticsViewMode viewMode) {
			Task retval = null;
			lock (plotsSync) {
				if (subplots != null && subplots.statPlots != null)
					retval = lvqPlotDispatcher.BeginInvoke(() => {
						foreach (var plot in subplots.statPlots)
							if (LvqStatPlotFactory.IsCurrPlot(plot.Plot)) {
								plot.Plot.MetaData.Hidden = viewMode == StatisticsViewMode.MeanAndStderr;
								((VizLineSegments)((ITranformed<Point[]>)plot.Plot.Visualisation).Implementation).DashStyle = viewMode == StatisticsViewMode.CurrentOnly ? DashStyles.Solid : LvqStatPlotFactory.CurrPlotDashStyle;
							} else
								plot.Plot.MetaData.Hidden = viewMode == StatisticsViewMode.CurrentOnly;
					}).AsTask();
				currViewMode = viewMode;
			}
			QueueUpdate();
			return retval ?? SeedUtils.CompletedTask();
		}

		public void ShowPrototypes(bool visible) {
			lock (plotsSync)
				if (subplots != null && subplots.classBoundaries != null)
					lvqPlotDispatcher.BeginInvoke(() => {
						if (subplots != null && subplots.classBoundaries != null)
							foreach (var protoPlot in subplots.prototypeClouds)
								protoPlot.Plot.MetaData.Hidden = !visible;
					});
			QueueUpdate();
		}

		public void ShowTestEmbedding(bool showTestEmbedding) {
			lock (plotsSync)
				if (subplots != null && subplots.showTestEmbedding != showTestEmbedding) {
					subplots.showTestEmbedding = showTestEmbedding;
					QueueUpdate();
				}
		}

		public void ShowTestErrorRates(bool showTestErrorRates) {
			lock (plotsSync)
				if (subplots != null && subplots.plots != null)
					lvqPlotDispatcher.BeginInvoke(() => {
						if (subplots != null && subplots.plots != null)
							foreach (var statPlot in subplots.plots.SelectMany(plot => plot.Graphs).Where(LvqStatPlotFactory.IsTestPlot)) 
								statPlot.MetaData.Hidden = !showTestErrorRates;
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
		readonly bool hide;
		public LvqStatPlotsContainer(CancellationToken exitToken, bool hide = false) {
			this.exitToken = exitToken;
			this.hide = hide;
			lvqPlotDispatcher = WpfTools.StartNewDispatcher(ThreadPriority.BelowNormal);
			lvqPlotTaskScheduler = lvqPlotDispatcher.GetScheduler().Result;
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
				Content = new Grid(),
				Visibility = hide ? Visibility.Hidden : Visibility.Visible
			};

			subPlotWindow.SizeChanged += (o, e) => RelayoutSubPlotWindow();
			subPlotWindow.Show();
			subPlotWindow.Top = 0;
			double subWindowLeft = subPlotWindow.Left = SystemParameters.FullPrimaryScreenWidth - subPlotWindow.Width;

			if (Application.Current != null) // just a little nicer layout; this won't work from F#
				Application.Current.Dispatcher.BeginInvoke(() => {
					var mainWindow = Application.Current.MainWindow;
					if (mainWindow != null && mainWindow.Left + mainWindow.Width > subWindowLeft)
						mainWindow.Left = Math.Max(0, subWindowLeft - +mainWindow.Width);
				});
		}

		public void QueueUpdate() { ThreadPool.QueueUserWorkItem(UpdateQueueProcessor); }
		readonly UpdateSync updateSync = new UpdateSync();

		void UpdateQueueProcessor(object _) {
			if (exitToken.IsCancellationRequested || !updateSync.UpdateEnqueue_IsMyTurn())
				return;

			LvqStatPlots currsubplots;
			lock (plotsSync) currsubplots = subplots;
			Task displayUpdateTask = GetDisplayUpdateTask(currsubplots);

			if (displayUpdateTask == null) { if (!updateSync.UpdateDone_IsQueueEmpty()) QueueUpdate(); } else displayUpdateTask.ContinueWith(task => { if (!updateSync.UpdateDone_IsQueueEmpty()) QueueUpdate(); });
		}

		static Task GetDisplayUpdateTask(LvqStatPlots currsubplots) {
			return DisplayUpdateOperations(currsubplots)
				.Aggregate(default(Task),
						   (current, currentOp) => current == null
													? Task.Factory.StartNew((Action)(() => currentOp.Wait()))
													: current.ContinueWith(task => currentOp.Wait())
				);
		}

		static IEnumerable<DispatcherOperation> DisplayUpdateOperations(LvqStatPlots subplots) {
			if (subplots != null)
				lock (subplots) {
					var projectionAndImage = subplots.CurrentProjection();

					if (projectionAndImage != null && subplots.prototypeClouds != null)
						yield return subplots.scatterPlotControl.Dispatcher.BeginInvokeBackground(
							() => {
								subplots.SetScatterBounds(projectionAndImage.Bounds);
								subplots.classBoundaries.ChangeData(projectionAndImage);
								subplots.dataClouds.ChangeData(projectionAndImage);
								for (int i = 0; i < subplots.prototypeClouds.Length; ++i) {
									subplots.prototypeClouds[i].ChangeData(projectionAndImage);
								}
							});

					var graphOperationsLazy =
						from plot in subplots.statPlots
						group plot by plot.Dispatcher into plotgroup
						select plotgroup.Key.BeginInvokeBackground(() => { foreach (var sp in plotgroup) sp.ChangeData(subplots); });

					foreach (var op in graphOperationsLazy) yield return op;
				}
		}

		public void Dispose() {
			lvqPlotDispatcher.Invoke(new Action(() => {
				subPlotWindow.Close();
				lvqPlotDispatcher.InvokeShutdown();
			}));
		}

		public static void QueueUpdateIfCurrent(LvqStatPlotsContainer plotData) {
			if (plotData != null && plotData.subplots != null)
				plotData.QueueUpdate();
		}

		public static readonly DirectoryInfo outputDir = FSUtil.FindDataDir(@"uni\Thesis\doc\plots", typeof(LvqStatPlotsContainer));
		public static DirectoryInfo AutoPlotDir { get { return outputDir.CreateSubdirectory("auto"); } }

		public static bool AnnouncePlotGeneration(LvqDatasetCli dataset, LvqModelSettingsCli shorthand, long iterIntent) {
			DirectoryInfo modelDir = GraphDir(dataset, shorthand);
			string iterPostfix = "-" + TestLr.ItersPrefix(iterIntent);

			string statspath = modelDir.FullName + "\\fullstats" + iterPostfix + ".txt";
			bool exists = File.Exists(statspath);
			if (!exists)
				File.WriteAllText(statspath, "");
			return !exists;
		}

		static DirectoryInfo GraphDir(LvqDatasetCli dataset, LvqModelSettingsCli modelSettings) {
			DirectoryInfo autoDir = outputDir.CreateSubdirectory(@"auto");
			string dSettingsShorthand = CanonicalizeDatasetShorthand(dataset.DatasetLabel);
			DirectoryInfo datasetDir = autoDir.GetDirectories()
				.FirstOrDefault(dir => CanonicalizeDatasetShorthand(dir.Name) == dSettingsShorthand)
				?? autoDir.CreateSubdirectory(dSettingsShorthand);
			string mSettingsShorthand = modelSettings.ToShorthand();
			return datasetDir.GetDirectories().FirstOrDefault(dir => CanonicalizeModelShorthand(dir.Name) == mSettingsShorthand) 
				?? datasetDir.CreateSubdirectory(mSettingsShorthand);
		}

		static string CanonicalizeDatasetShorthand(string shorthand)
		{
			var dSettings = CreateDataset.CreateFactory(shorthand);
			return dSettings == null ? shorthand : dSettings.Shorthand;
		}
		static string CanonicalizeModelShorthand(string shorthand) {
			var otherSettings = CreateLvqModelValues.TryParseShorthand(shorthand);
			return otherSettings.HasValue ? otherSettings.Value.ToShorthand() : shorthand;
		}

		public Task SaveAllGraphs(bool alsoEmbedding) {
			return Task.Factory.StartNew(() => GetDisplayUpdateTask(subplots).Wait()).ContinueWith(_ => {

				if (subplots == null) { Console.WriteLine("No plots to save!"); return; }

				DirectoryInfo modelDir = GraphDir(subplots.dataset, CreateLvqModelValues.ParseShorthand(subplots.model.ModelLabel));
				double iterations = subplots.model.CurrentRawStats(subplots.dataset).Value[LvqTrainingStatCli.TrainingIterationI];
				string iterPostfix = "-" + TestLr.ItersPrefix((long)(iterations + 0.5));

				Grid plotGrid = (Grid)subPlotWindow.Content;
				PlotControl[] plotControls = plotGrid.Children.OfType<PlotControl>().ToArray();
				foreach (var plotControl in plotControls) {
					if (plotControl.PlotName == "embed" && !alsoEmbedding)
						continue;
					string filename = plotnameLookup(plotControl.PlotName)
						+ (currViewMode == StatisticsViewMode.CurrentOnly ? @"-c" : currViewMode == StatisticsViewMode.CurrentAndMean ? @"-cm" : @"-m")
						+ iterPostfix
						+ ".xps";
					string filepath = modelDir.FullName + "\\" + filename;
					File.WriteAllBytes(filepath, plotControl.PrintToByteArray());
					Console.Write(".");
				}
				File.WriteAllText(modelDir.FullName + "\\stats" + iterPostfix + ".txt", subplots.model.CurrentStatsString(subplots.dataset));
				File.WriteAllText(modelDir.FullName + "\\fullstats" + iterPostfix + ".txt", subplots.model.CurrentFullStatsString(subplots.dataset));
				Console.Write(";");
			}, lvqPlotTaskScheduler);
		}

		static string plotnameLookup(string fullname) {
			switch (fullname) {
				case "Border Matrix absolute determinant": return "Bdet";
				case "Border Matrix norm": return "Bnorm";
				case "Projection Matrix":
				case "Prototype Matrix": return "Pnorm";
				case "Cost Function": return "cost";
				case "Cumulative Learning Rates": return "cumullr";
				case "Cumulative μ-scaled Learning Rates": return "cumulmu";
				case "Prototype Distance": return "dist";
				case "Prototype Distance Variance": return "distVar";
				case "Error Rates": return "err";
				case "embed": return "embed";
				case "Projection Quality": return "nn";
				case "Prototype bias": return "bias";
				case "max μ": return "maxmu";
				case "mean μ": return "meanmu";
				default: return fullname;
			}
		}


	}
}
