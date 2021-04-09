// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using EmnExtensions.Threading;
using LvqLibCli;
using EmnExtensions.Wpf;
using LvqGui.LvqPlotting;

namespace LvqGui.CoreGui
{
    public sealed partial class LvqWindow
    {
        readonly CancellationTokenSource cts = new();

        public CancellationToken ClosingToken
            => cts.Token;

        public LvqWindow()
        {
            using (var proc = Process.GetCurrentProcess()) {
                proc.PriorityClass = ProcessPriorityClass.BelowNormal;
            }

            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            var windowValues = new LvqWindowValues(this);
            DataContext = windowValues;
            InitializeComponent();
            windowValues.TrainingControlValues.SelectedModelUpdatedInBackgroundThread += TrainingControlValues_SelectedModelUpdatedInBackgroundThread;
            windowValues.TrainingControlValues.PropertyChanged += TrainingControlValues_PropertyChanged;
            Closing += (o, e) => {
                windowValues.TrainingControlValues.AnimateTraining = false;
            };
#if BENCHMARK
            this.Loaded += (o, e) => DoBenchmark();
#endif
            Closed += LvqWindow_Closed;
        }

        LvqWindowValues Values
            => (LvqWindowValues)DataContext;

        void LvqWindow_Closed(object sender, EventArgs e)
        {
            cts.Cancel();
            if (lvqPlotContainer != null) {
                lvqPlotContainer.Dispose();
                lvqPlotContainer = null;
            }

            LvqMultiModel.WaitForTraining();
            var windowValues = (LvqWindowValues)DataContext;
            windowValues.LvqModels.Clear();
            windowValues.Datasets.Clear();
            GC.Collect();
            Dispatcher.BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        // ReSharper disable UnusedMember.Local
        void DoBenchmark()
            => ThreadPool.QueueUserWorkItem(
                o => {
                    var values = (LvqWindowValues)o;
                    values.CreateStarDatasetValues.ParamsSeed = 1337;
                    values.CreateStarDatasetValues.InstanceSeed = 37;

                    values.CreateLvqModelValues.ParamsSeed = 42;
                    values.CreateLvqModelValues.InstanceSeed = 1234;

                    values.CreateStarDatasetValues.ConfirmCreation().Completed +=
                        (s, e) => Dispatcher.BeginInvokeBackground(
                            () => values.CreateLvqModelValues.ConfirmCreation().ContinueWith(
                                creationTask => Dispatcher.BeginInvokeBackground(
                                    () => {
                                        values.TrainingControlValues.AnimateTraining = true;
                                    }
                                )
                            )
                        );
                },
                DataContext
            );

        LvqStatPlotsContainer lvqPlotContainer;

        void TrainingControlValues_SelectedModelUpdatedInBackgroundThread()
            => LvqStatPlotsContainer.QueueUpdateIfCurrent(lvqPlotContainer);

        void TrainingControlValues_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ShowBoundaries" && lvqPlotContainer != null) {
                lvqPlotContainer.ShowBoundaries(Values.TrainingControlValues.ShowBoundaries);
            }

            if (e.PropertyName == "ShowPrototypes" && lvqPlotContainer != null) {
                lvqPlotContainer.ShowPrototypes(Values.TrainingControlValues.ShowPrototypes);
            }

            if (e.PropertyName == "ShowTestEmbedding" && lvqPlotContainer != null) {
                lvqPlotContainer.ShowTestEmbedding(Values.TrainingControlValues.ShowTestEmbedding);
            }

            if (e.PropertyName == "ShowTestErrorRates" && lvqPlotContainer != null) {
                lvqPlotContainer.ShowTestErrorRates(Values.TrainingControlValues.ShowTestErrorRates);
            }

            if (e.PropertyName == "ShowNnErrorRates" && lvqPlotContainer != null) {
                lvqPlotContainer.ShowNnErrorRates(Values.TrainingControlValues.ShowNnErrorRates);
            }

            if (e.PropertyName == "CurrProjStats" && lvqPlotContainer != null) {
                lvqPlotContainer.ShowCurrentProjectionStats(Values.TrainingControlValues.CurrProjStats);
            }

            if (e.PropertyName == "SelectedDataset" || e.PropertyName == "SelectedLvqModel" || e.PropertyName == "SubModelIndex") {
                ModelChanged();
            }
        }

        void ModelChanged()
        {
            if (lvqPlotContainer == null && Values.TrainingControlValues.SelectedDataset != null && Values.TrainingControlValues.SelectedLvqModel != null) {
                lvqPlotContainer = new(ClosingToken);
            }

            if (lvqPlotContainer != null) {
                lvqPlotContainer.DisplayModel(Values.TrainingControlValues.SelectedDataset, Values.TrainingControlValues.SelectedLvqModel, Values.TrainingControlValues.SubModelIndex, Values.TrainingControlValues.CurrProjStats, Values.TrainingControlValues.ShowBoundaries, Values.TrainingControlValues.ShowPrototypes, Values.TrainingControlValues.ShowTestEmbedding, Values.TrainingControlValues.ShowTestErrorRates);
            }
        }

        public bool Fullscreen
        {
            get => (bool)GetValue(FullscreenProperty);
            set => SetValue(FullscreenProperty, value);
        }

        WindowState lastState = WindowState.Normal;

        // Using a DependencyProperty as the backing store for Fullscreen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FullscreenProperty =
            DependencyProperty.RegisterAttached(
                "Fullscreen",
                typeof(bool),
                typeof(LvqWindow),
                new UIPropertyMetadata(
                    false,
                    (o, e) => {
                        var win = (LvqWindow)o;
                        if ((bool)e.NewValue) {
                            win.lastState = win.WindowState;
                            win.WindowState = WindowState.Normal;
                            win.WindowStyle = WindowStyle.None;
                            win.Topmost = true;
                            win.WindowState = WindowState.Maximized;
                        } else {
                            win.Topmost = false;
                            win.WindowStyle = WindowStyle.SingleBorderWindow;
                            win.WindowState = win.lastState;
                        }
                    }
                )
            );

        // ReSharper disable MemberCanBeMadeStatic.Global
        public IEnumerable<LvqModelType> ModelTypes
            => (LvqModelType[])Enum.GetValues(typeof(LvqModelType));

        public IEnumerable<long> Iters
            => new[] { 100000L, 1000000L, 10000000L, };
        // ReSharper restore MemberCanBeMadeStatic.Global

        public Task SaveAllGraphs()
        {
            _ = Values.TrainingControlValues.SelectedLvqModel;
            var allmodels = Values.TrainingControlValues.MatchingLvqModels.ToArray();
            var graphSettings = new { Values.TrainingControlValues.ShowBoundaries, Values.TrainingControlValues.ShowPrototypes, Values.TrainingControlValues.CurrProjStats, Values.TrainingControlValues.ShowTestEmbedding, Values.TrainingControlValues.ShowTestErrorRates };
            var lvqInnerPlotContainer = new LvqStatPlotsContainer(ClosingToken, true);

            var done = new TaskCompletionSource<object>();
            done.SetResult(null);
            Task doneTask = done.Task;

            var counter = 0;
            Console.WriteLine("Saving " + allmodels.Length + " model graphs:");
            return
                allmodels.Aggregate(
                    doneTask,
                    (task, model) =>
                        task.Then(() => lvqInnerPlotContainer.DisplayModel(model.InitSet, model, model.SelectedSubModel, StatisticsViewMode.CurrentOnly, graphSettings.ShowBoundaries, graphSettings.ShowPrototypes, graphSettings.ShowTestEmbedding, graphSettings.ShowTestErrorRates))
                            .Then(() => lvqInnerPlotContainer.SaveAllGraphs(true))
                            .Then(() => lvqInnerPlotContainer.SaveAllGraphs(false))
                            .Then(() => lvqInnerPlotContainer.DisplayModel(model.InitSet, model, model.SelectedSubModel, StatisticsViewMode.MeanAndStderr, graphSettings.ShowBoundaries, graphSettings.ShowPrototypes, graphSettings.ShowTestEmbedding, graphSettings.ShowTestErrorRates))
                            .Then(() => lvqInnerPlotContainer.SaveAllGraphs(false))
                            .ContinueWith(
                                previousTask => {
                                    _ = Interlocked.Increment(ref counter);
                                    Console.WriteLine(counter + "/" + allmodels.Length);
                                }
                            )
                ).ContinueWith(
                    t => {
                        if (t.Status == TaskStatus.Faulted) {
                            Console.WriteLine(t.Exception);
                        }

                        lvqInnerPlotContainer.Dispose();
                        Console.WriteLine("saved.");
                    }
                );
        }
    }
}
