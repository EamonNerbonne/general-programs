using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using EmnExtensions.Filesystem;
using EmnExtensions.Wpf;
using EmnExtensions.Wpf.Plot;
using EmnExtensions.Wpf.Plot.VizEngines;
using EmnExtensions.Wpf.WpfTools;
using LvqGui.CreatorGui;
using LvqLibCli;

namespace LvqGui.LvqPlotting
{
    public sealed class LvqStatPlotsContainer : IDisposable
    {
        readonly object plotsSync = new();
        LvqStatPlots subplots;
        readonly Dispatcher lvqPlotDispatcher;
        readonly TaskScheduler lvqPlotTaskScheduler; //corresponds to lvqPlotDispatcher

        public Task DisplayModel(
            LvqDatasetCli dataset,
            LvqMultiModel model,
            int new_subModelIdx,
            StatisticsViewMode viewMode,
            bool showBoundaries,
            bool showPrototypes,
            bool showTestEmbedding,
            bool showTestErrorRates)
        {
            if (lvqPlotDispatcher.HasShutdownStarted) {
                throw new InvalidOperationException("Dispatcher shutting down");
            }

            return lvqPlotTaskScheduler.StartNewTask(
                () => {
                    lock (plotsSync) {
                        if (dataset == null || model == null) {
                            subPlotWindow.Title = "No Model Selected";
                            subplots = null;
                        } else {
                            MakeSubPlotWindow();
                            subPlotWindow.Title = model.ModelLabel;
                            var oldsubplots = model.Tag as LvqStatPlots;

                            var modelChange = oldsubplots == null || oldsubplots.dataset != dataset || oldsubplots.model != model || oldsubplots.plots.First().Dispatcher != lvqPlotDispatcher;
                            if (modelChange) {
                                model.Tag = subplots = new(dataset, model);
                            } else {
                                subplots = oldsubplots;
                            }

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
                }
            );
        }

        public void ShowBoundaries(bool visible)
        {
            lock (plotsSync) {
                if (subplots != null && subplots.classBoundaries != null) {
                    DispatcherUtils.BeginInvoke(
                        lvqPlotDispatcher,
                        () => {
                            if (subplots != null && subplots.classBoundaries != null) {
                                subplots.classBoundaries.MetaData.Hidden = !visible;
                            }
                        }
                    );
                }
            }

            QueueUpdate();
        }

        StatisticsViewMode currViewMode;

        public Task ShowCurrentProjectionStats(StatisticsViewMode viewMode)
        {
            Task retval = null;
            lock (plotsSync) {
                if (subplots != null && subplots.statPlots != null) {
                    retval = DispatcherUtils.BeginInvoke(
                        lvqPlotDispatcher,
                        () => {
                            foreach (var plot in subplots.statPlots) {
                                if (LvqStatPlotFactory.IsCurrPlot(plot)) {
                                    plot.MetaData.Hidden = viewMode == StatisticsViewMode.MeanAndStderr
                                        || !currShowTestErrorRates && LvqStatPlotFactory.IsTestPlot(plot)
                                        || !currShowNnErrorRates && LvqStatPlotFactory.IsNnPlot(plot)
                                        ;
                                    ((VizLineSegments)((ITranformed<Point[]>)plot).Implementation).DashStyle = viewMode == StatisticsViewMode.CurrentOnly ? DashStyles.Solid : LvqStatPlotFactory.CurrPlotDashStyle;
                                } else {
                                    plot.MetaData.Hidden = viewMode == StatisticsViewMode.CurrentOnly
                                        || !currShowTestErrorRates && LvqStatPlotFactory.IsTestPlot(plot)
                                        || !currShowNnErrorRates && LvqStatPlotFactory.IsNnPlot(plot)
                                        ;
                                }
                            }
                        }
                    ).AsTask();
                }

                currViewMode = viewMode;
            }

            QueueUpdate();
            return retval ?? DispatcherUtils.CompletedTask();
        }

        public void ShowPrototypes(bool visible)
        {
            lock (plotsSync) {
                if (subplots != null && subplots.classBoundaries != null) {
                    DispatcherUtils.BeginInvoke(
                        lvqPlotDispatcher,
                        () => {
                            if (subplots != null && subplots.classBoundaries != null) {
                                foreach (var protoPlot in subplots.prototypeClouds) {
                                    protoPlot.MetaData.Hidden = !visible;
                                }
                            }
                        }
                    );
                }
            }

            QueueUpdate();
        }

        public void ShowTestEmbedding(bool showTestEmbedding)
        {
            lock (plotsSync) {
                if (subplots != null && subplots.showTestEmbedding != showTestEmbedding) {
                    subplots.showTestEmbedding = showTestEmbedding;
                    QueueUpdate();
                }
            }
        }

        bool currShowTestErrorRates;

        public void ShowTestErrorRates(bool showTestErrorRates)
        {
            currShowTestErrorRates = showTestErrorRates;
            lock (plotsSync) {
                if (subplots != null && subplots.plots != null) {
                    DispatcherUtils.BeginInvoke(
                        lvqPlotDispatcher,
                        () => {
                            if (subplots != null && subplots.plots != null) {
                                foreach (var plot in subplots.plots.SelectMany(plot => plot.Graphs).Where(LvqStatPlotFactory.IsTestPlot)) {
                                    plot.MetaData.Hidden = !showTestErrorRates
                                        || currViewMode == (LvqStatPlotFactory.IsCurrPlot(plot) ? StatisticsViewMode.MeanAndStderr : StatisticsViewMode.CurrentOnly);
                                }
                            }
                        }
                    );
                }
            }

            QueueUpdate();
        }

        bool currShowNnErrorRates;

        public void ShowNnErrorRates(bool showNnErrorRates)
        {
            currShowNnErrorRates = showNnErrorRates;
            lock (plotsSync) {
                if (subplots != null && subplots.plots != null) {
                    DispatcherUtils.BeginInvoke(
                        lvqPlotDispatcher,
                        () => {
                            if (subplots != null && subplots.plots != null) {
                                foreach (var plot in subplots.plots.SelectMany(plot => plot.Graphs).Where(LvqStatPlotFactory.IsNnPlot)) {
                                    plot.MetaData.Hidden = !showNnErrorRates
                                        || currViewMode == (LvqStatPlotFactory.IsCurrPlot(plot) ? StatisticsViewMode.MeanAndStderr : StatisticsViewMode.CurrentOnly);
                                }
                            }
                        }
                    );
                }
            }

            QueueUpdate();
        }

        void RelayoutSubPlotWindow(bool resetChildrenFirst = false)
        {
            var plotGrid = (Grid)subPlotWindow.Content;
            if (subplots == null) {
                plotGrid.Children.Clear();
                return;
            }

            var ratio = subPlotWindow.ActualWidth / subPlotWindow.ActualHeight;

            if (resetChildrenFirst) {
                plotGrid.Children.Clear();
                if (subplots.scatterPlotControl != null) {
                    plotGrid.Children.Add(subplots.scatterPlotControl);
                }

                foreach (var plot in subplots.plots) {
                    plotGrid.Children.Add(plot);
                }

                BuildContextMenu(plotGrid.Children.Cast<PlotControl>());
                foreach (PlotControl plot in plotGrid.Children) {
                    plot.Margin = new(2.0);
                    plot.Background = Brushes.White;
                }
            }

            var visibleChildren = plotGrid.Children.Cast<PlotControl>().Where(plot => plot.Visibility == Visibility.Visible).ToArray();
            var plotCount = visibleChildren.Length;

            var layout = (
                from CellsWide in Enumerable.Range((int)Math.Sqrt(plotCount * ratio), 2)
                from CellsHigh in Enumerable.Range((int)Math.Sqrt(plotCount / ratio), 2)
                where CellsWide * CellsHigh >= plotCount
                orderby CellsWide * CellsHigh
                select new { CellsWide, CellsHigh }
            ).First();

            var unitLength = new GridLength(1.0, GridUnitType.Star);
            while (plotGrid.ColumnDefinitions.Count < layout.CellsWide) {
                plotGrid.ColumnDefinitions.Add(new() { Width = unitLength });
            }

            if (plotGrid.ColumnDefinitions.Count > layout.CellsWide) {
                plotGrid.ColumnDefinitions.RemoveRange(layout.CellsWide, plotGrid.ColumnDefinitions.Count - layout.CellsWide);
            }

            while (plotGrid.RowDefinitions.Count < layout.CellsHigh) {
                plotGrid.RowDefinitions.Add(new() { Height = unitLength });
            }

            if (plotGrid.RowDefinitions.Count > layout.CellsHigh) {
                plotGrid.RowDefinitions.RemoveRange(layout.CellsHigh, plotGrid.RowDefinitions.Count - layout.CellsHigh);
            }

            for (var i = 0; i < visibleChildren.Length; ++i) {
                Grid.SetRow(visibleChildren[i], i / layout.CellsWide);
                Grid.SetColumn(visibleChildren[i], i % layout.CellsWide);
            }
        }

        readonly HashSet<string> VisiblePlots = new(new[] { "embed", "NN Error", "Error Rates" });

        void BuildContextMenu(IEnumerable<PlotControl> plots)
        {
            var items = new List<MenuItem>();
            foreach (var plot in plots) {
                var visible = VisiblePlots.Contains(plot.PlotName);
                plot.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                var menuItem = new MenuItem {
                    Header = plot.PlotName,
                    IsCheckable = true,
                    IsChecked = plot.Visibility == Visibility.Visible,
                    Tag = plot
                };
                menuItem.Checked += menuitem_Checked;
                menuItem.Unchecked += menuitem_Checked;
                items.Add(menuItem);
            }

            subPlotWindow.ContextMenu = new() { ItemsSource = items.ToArray() };
        }

        void menuitem_Checked(object sender, RoutedEventArgs e)
        {
            var item = (MenuItem)sender;
            var plot = (PlotControl)item.Tag;
            plot.Visibility = item.IsChecked ? Visibility.Visible : Visibility.Collapsed;
            if (item.IsChecked) {
                VisiblePlots.Add(plot.PlotName);
            } else {
                VisiblePlots.Remove(plot.PlotName);
            }

            RelayoutSubPlotWindow();
        }

        Window subPlotWindow;
        readonly CancellationToken exitToken;
        readonly bool hide;

        public LvqStatPlotsContainer(CancellationToken exitToken, bool hide = false)
        {
            this.exitToken = exitToken;
            this.hide = hide;
            lvqPlotDispatcher = WpfTools.StartNewDispatcher(ThreadPriority.BelowNormal);
            lvqPlotTaskScheduler = lvqPlotDispatcher.GetScheduler().Result;
            DispatcherUtils.BeginInvoke(lvqPlotDispatcher, MakeSubPlotWindow);
            exitToken.Register(() => lvqPlotDispatcher.InvokeShutdown());
        }

        void MakeSubPlotWindow()
        {
            var borderWidth = (SystemParameters.MaximizedPrimaryScreenWidth - SystemParameters.FullPrimaryScreenWidth) / 2.0;
            if (subPlotWindow != null && subPlotWindow.IsLoaded) {
                if (!hide) {
                    subPlotWindow.Show();
                }

                return;
            }

            subPlotWindow = new() {
                Width = SystemParameters.FullPrimaryScreenWidth * 0.7,
                Height = SystemParameters.MaximizedPrimaryScreenHeight - borderWidth * 2,
                Title = "No Model Selected",
                Background = Brushes.Gray,
                Content = new Grid(),
                Visibility = hide ? Visibility.Hidden : Visibility.Visible
            };
            subPlotWindow.Closing += subPlotWindow_Closing;
            subPlotWindow.SizeChanged += (o, e) => RelayoutSubPlotWindow();
            subPlotWindow.Show();
            subPlotWindow.Top = 0;
            var subWindowLeft = subPlotWindow.Left = SystemParameters.FullPrimaryScreenWidth - subPlotWindow.Width;

            if (Application.Current != null) // just a little nicer layout; this won't work from F#
            {
                DispatcherUtils.BeginInvoke(
                    Application.Current.Dispatcher,
                    () => {
                        var mainWindow = Application.Current.MainWindow;
                        if (mainWindow != null && mainWindow.Left + mainWindow.Width > subWindowLeft) {
                            mainWindow.Left = Math.Max(0, subWindowLeft - +mainWindow.Width);
                        }
                    }
                );
            }
        }

        void subPlotWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!reallyClosing) {
                e.Cancel = true;
                subPlotWindow.Hide();
            }
        }

        public void QueueUpdate()
            => ThreadPool.QueueUserWorkItem(UpdateQueueProcessor);

        readonly UpdateSync updateSync = new();

        void UpdateQueueProcessor(object _)
        {
            if (exitToken.IsCancellationRequested || !updateSync.UpdateEnqueue_IsMyTurn()) {
                return;
            }

            LvqStatPlots currsubplots;
            lock (plotsSync) {
                currsubplots = subplots;
            }

            var displayUpdateTask = GetDisplayUpdateTask(currsubplots);

            if (displayUpdateTask == null) {
                if (!updateSync.UpdateDone_IsQueueEmpty()) {
                    QueueUpdate();
                }
            } else {
                displayUpdateTask.ContinueWith(
                    task => {
                        if (!updateSync.UpdateDone_IsQueueEmpty()) {
                            QueueUpdate();
                        }
                    }
                );
            }
        }

        static Task GetDisplayUpdateTask(LvqStatPlots currsubplots)
            => DisplayUpdateOperations(currsubplots)
                .Aggregate(
                    default(Task),
                    (current, currentOp) => current?.ContinueWith(task => currentOp.Wait()) ?? Task.Factory.StartNew((Action)(() => currentOp.Wait()))
                );

        static IEnumerable<DispatcherOperation> DisplayUpdateOperations(LvqStatPlots subplots)
        {
            if (subplots != null) {
                lock (subplots) {
                    var projectionAndImage = subplots.CurrentProjection();

                    if (projectionAndImage != null && subplots.prototypeClouds != null) {
                        yield return subplots.scatterPlotControl.Dispatcher.BeginInvokeBackground(
                            () => {
                                subplots.SetScatterBounds(projectionAndImage.Bounds);
                                subplots.classBoundaries.ChangeData(projectionAndImage);
                                subplots.dataClouds.ChangeData(projectionAndImage);
                                for (var i = 0; i < subplots.prototypeClouds.Length; ++i) {
                                    subplots.prototypeClouds[i].ChangeData(projectionAndImage);
                                }
                            }
                        );
                    }

                    var graphOperationsLazy =
                        from plot in subplots.statPlots
                        group plot by plot.GetDispatcher()
                        into plotgroup
                        select (plotgroup.Key ?? subplots.plots[0].Dispatcher).BeginInvokeBackground(
                            () => {
                                foreach (var sp in plotgroup) {
                                    sp.ChangeData(subplots);
                                }
                            }
                        );

                    foreach (var op in graphOperationsLazy) {
                        yield return op;
                    }
                }
            }
        }

        bool reallyClosing;

        public void Dispose()
        {
            reallyClosing = true;
            if (!lvqPlotDispatcher.HasShutdownStarted) {
                lvqPlotDispatcher.Invoke(
                    () => {
                        subPlotWindow.Close();
                        lvqPlotDispatcher.InvokeShutdown();
                    }
                );
            }
        }

        public static void QueueUpdateIfCurrent(LvqStatPlotsContainer plotData)
        {
            if (plotData != null && plotData.subplots != null) {
                plotData.QueueUpdate();
            }
        }

        public static readonly DirectoryInfo outputDir = FSUtil.FindDataDir(@"uni\Thesis\doc\plots", typeof(LvqStatPlotsContainer));

        public static DirectoryInfo AutoPlotDir
            => outputDir.CreateSubdirectory("auto");

        public static bool AnnouncePlotGeneration(LvqDatasetCli dataset, LvqModelSettingsCli shorthand, long iterIntent)
        {
            var modelDir = GraphDir(dataset, shorthand);
            var iterPostfix = "-" + LvqMultiModel.ItersPrefix(iterIntent);

            var statspath = modelDir.FullName + "\\fullstats" + iterPostfix + ".txt";
            var exists = File.Exists(statspath);
            if (!exists) {
                File.WriteAllText(statspath, "");
            }

            return !exists;
        }

        static DirectoryInfo GraphDir(LvqDatasetCli dataset, LvqModelSettingsCli modelSettings)
        {
            var autoDir = outputDir.CreateSubdirectory(@"auto");
            var dSettingsShorthand = CanonicalizeDatasetShorthand(dataset.DatasetLabel);
            var datasetDir = autoDir.GetDirectories()
                    .FirstOrDefault(dir => CanonicalizeDatasetShorthand(dir.Name) == dSettingsShorthand)
                ?? autoDir.CreateSubdirectory(dSettingsShorthand);
            var mSettingsShorthand = modelSettings.ToShorthand();
            return datasetDir.GetDirectories().FirstOrDefault(dir => CanonicalizeModelShorthand(dir.Name) == mSettingsShorthand)
                ?? datasetDir.CreateSubdirectory(mSettingsShorthand);
        }

        static string CanonicalizeDatasetShorthand(string shorthand)
        {
            var dSettings = CreateDataset.CreateFactory(shorthand);
            return dSettings == null ? shorthand : dSettings.Shorthand;
        }

        static string CanonicalizeModelShorthand(string shorthand)
        {
            var otherSettings = CreateLvqModelValues.TryParseShorthand(shorthand);
            return otherSettings.HasValue ? otherSettings.Value.ToShorthand() : shorthand;
        }

        public Task SaveAllGraphs(bool alsoEmbedding)
            => Task.Factory.StartNew(() => GetDisplayUpdateTask(subplots).Wait()).ContinueWith(
                _ => {
                    if (subplots == null) {
                        Console.WriteLine("No plots to save!");
                        return;
                    }

                    var modelDir = GraphDir(subplots.dataset, CreateLvqModelValues.ParseShorthand(subplots.model.ModelLabel));
                    var iterations = subplots.model.CurrentRawStats().Value[LvqTrainingStatCli.TrainingIterationI];
                    var iterPostfix = "-" + LvqMultiModel.ItersPrefix((long)(iterations + 0.5));

                    var plotGrid = (Grid)subPlotWindow.Content;
                    var plotControls = plotGrid.Children.OfType<PlotControl>().ToArray();
                    foreach (var plotControl in plotControls) {
                        if (plotControl.PlotName == "embed" && !alsoEmbedding) {
                            continue;
                        }

                        var filename = plotnameLookup(plotControl.PlotName)
                            + (currViewMode == StatisticsViewMode.CurrentOnly
                                ? @"-c"
                                : currViewMode == StatisticsViewMode.CurrentAndMean
                                    ? @"-cm"
                                    : @"-m")
                            + iterPostfix
                            + ".xps";
                        var filepath = modelDir.FullName + "\\" + filename;
                        File.WriteAllBytes(filepath, plotControl.PrintToByteArray());
                        Console.Write(".");
                    }

                    File.WriteAllText(modelDir.FullName + "\\stats" + iterPostfix + ".txt", subplots.model.CurrentStatsString());
                    File.WriteAllText(modelDir.FullName + "\\fullstats" + iterPostfix + ".txt", subplots.model.CurrentFullStatsString());
                    Console.Write(";");
                },
                lvqPlotTaskScheduler
            );

        static string plotnameLookup(string fullname)
        {
            switch (fullname) {
                case "Border Matrix absolute determinant":
                case "Border Matrix: log(abs(|B|))":
                    return "Bdet";
                case "Border Matrix: log(||B||^2)":
                case "B-norm: log(||B||^2)":
                case "Border Matrix norm":
                    return "Bnorm";
                case "Projection Norm":
                case "Prototype Matrix":
                    return "Pnorm";
                case "Cost Function":
                    return "cost";
                case "Cumulative Learning Rates":
                    return "cumullr";
                case "Cumulative μ-scaled Learning Rates":
                    return "cumulmu";
                case "Prototype Distance":
                    return "dist";
                case "Prototype Distance Variance":
                    return "distVar";
                case "Error Rates":
                    return "err";
                case "embed":
                    return "embed";
                case "NN Error":
                    return "nn";
                case "Prototype bias":
                    return "bias";
                case "max μ":
                    return "maxmu";
                case "mean μ":
                    return "meanmu";
                default:
                    return fullname;
            }
        }
    }
}
