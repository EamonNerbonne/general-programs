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
using LvqLibCli;


namespace LvqGui {
	public sealed class LvqStatPlotsContainer : IDisposable {
		readonly UpdateSync updateSync = new UpdateSync();
		readonly object plotsSync = new object();
		LvqStatPlots subplots;

		readonly Dispatcher lvqPlotDispatcher;

		public void DisplayModel(LvqDatasetCli dataset, LvqMultiModel model, int new_subModelIdx, bool showSelectedModelGraphs, bool showBoundaries, bool showPrototypes) {
			lvqPlotDispatcher.BeginInvoke(() => {
				lock (plotsSync) {
					if (dataset == null || model == null) {
						subPlotWindow.Title = "No Model Selected";
						subplots = null;
					} else {
						MakeSubPlotWindow();
						subPlotWindow.Title = model.ModelLabel;
						LvqStatPlots oldsubplots = model.Tag as LvqStatPlots;

						bool modelChange = oldsubplots == null || oldsubplots.dataset != dataset || oldsubplots.model != model;
						if (modelChange)
							model.Tag = subplots = new LvqStatPlots(dataset, model);
						else
							subplots = oldsubplots;
						subplots.selectedSubModel = new_subModelIdx;
					}
				}
				ShowBoundaries(showBoundaries);
				ShowCurrentProjectionStats(showSelectedModelGraphs);
				ShowPrototypes(showPrototypes);
				RelayoutSubPlotWindow(true);
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

		public void ShowCurrentProjectionStats(bool visible) {
			lock (plotsSync)
				if (subplots != null && subplots.statPlots != null)
					lvqPlotDispatcher.BeginInvoke(() => {
						foreach (var plot in subplots.statPlots)
							if (plot.Plot.MetaData.Tag == LvqStatPlotFactory.IsCurrPlotTag)
								plot.Plot.MetaData.Hidden = !visible;
					});
			QueueUpdate();
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

		public LvqStatPlotsContainer(CancellationToken exitToken) {
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

		void QueueUpdate() { ThreadPool.QueueUserWorkItem(UpdateQueueProcessor); }
		void UpdateQueueProcessor(object _) {
			if (exitToken.IsCancellationRequested || !updateSync.UpdateEnqueue_IsMyTurn())
				return;
			LvqStatPlots currsubplots;

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

		static IEnumerable<DispatcherOperation> DisplayUpdateOperations(LvqStatPlots subplots) {
			if (subplots != null) {
				var projectionAndImage = subplots.CurrentProjection();

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

		internal static void QueueUpdateIfCurrent(LvqStatPlotsContainer plotData) {
			if (plotData != null && plotData.subplots != null)
				plotData.QueueUpdate();
		}
	}
}
