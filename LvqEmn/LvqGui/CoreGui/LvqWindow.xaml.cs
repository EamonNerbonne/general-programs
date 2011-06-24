using System;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using EmnExtensions.Wpf;
using LvqLibCli;

namespace LvqGui {
	public partial class LvqWindow {
		readonly CancellationTokenSource cts = new CancellationTokenSource();
		public CancellationToken ClosingToken { get { return cts.Token; } }
		public LvqWindow() {
			using (var proc = Process.GetCurrentProcess())
				proc.PriorityClass = ProcessPriorityClass.BelowNormal;

			Thread.CurrentThread.Priority = ThreadPriority.Highest;
			var windowValues = new LvqWindowValues(this);
			DataContext = windowValues;
			InitializeComponent();
			windowValues.TrainingControlValues.SelectedModelUpdatedInBackgroundThread += TrainingControlValues_SelectedModelUpdatedInBackgroundThread;
			windowValues.TrainingControlValues.PropertyChanged += TrainingControlValues_PropertyChanged;
			Closing += (o, e) => { windowValues.TrainingControlValues.AnimateTraining = false; };
#if BENCHMARK
			this.Loaded += (o, e) => DoBenchmark();
#endif
			Closed += LvqWindow_Closed;
		}

		LvqWindowValues Values { get { return (LvqWindowValues)DataContext; } }

		void LvqWindow_Closed(object sender, EventArgs e) {
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
		void DoBenchmark() {		// ReSharper restore UnusedMember.Local
			ThreadPool.QueueUserWorkItem(o => {
				LvqWindowValues values = ((LvqWindowValues)o);
				values.CreateStarDatasetValues.ParamsSeed = 1337;
				values.CreateStarDatasetValues.InstanceSeed = 37;

				values.CreateLvqModelValues.ParamsSeed = 42;
				values.CreateLvqModelValues.InstanceSeed = 1234;

				values.CreateStarDatasetValues.ConfirmCreation().Completed +=
						(s, e) => Dispatcher.BeginInvokeBackground(
							() => values.CreateLvqModelValues.ConfirmCreation().ContinueWith(
							creationTask => Dispatcher.BeginInvokeBackground(
							() => { values.TrainingControlValues.AnimateTraining = true; }
							)));

			}, DataContext);
		}

		LvqStatPlotsContainer lvqPlotContainer;
		void TrainingControlValues_SelectedModelUpdatedInBackgroundThread() {
			LvqStatPlotsContainer.QueueUpdateIfCurrent(lvqPlotContainer);
		}

		void TrainingControlValues_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "ShowBoundaries" && lvqPlotContainer != null)
				lvqPlotContainer.ShowBoundaries(Values.TrainingControlValues.ShowBoundaries);
			if (e.PropertyName == "ShowPrototypes" && lvqPlotContainer != null)
				lvqPlotContainer.ShowPrototypes(Values.TrainingControlValues.ShowPrototypes);
			if (e.PropertyName == "CurrProjStats" && lvqPlotContainer != null)
				lvqPlotContainer.ShowCurrentProjectionStats(Values.TrainingControlValues.CurrProjStats);
			if (e.PropertyName == "SelectedDataset" || e.PropertyName == "SelectedLvqModel" || e.PropertyName == "SubModelIndex")
				ModelChanged();
		}


		void ModelChanged() {
			if (lvqPlotContainer == null && Values.TrainingControlValues.SelectedDataset != null && Values.TrainingControlValues.SelectedLvqModel != null)
				lvqPlotContainer = new LvqStatPlotsContainer(ClosingToken);

			if (lvqPlotContainer != null)
				lvqPlotContainer.DisplayModel(Values.TrainingControlValues.SelectedDataset, Values.TrainingControlValues.SelectedLvqModel, Values.TrainingControlValues.SubModelIndex, Values.TrainingControlValues.CurrProjStats, Values.TrainingControlValues.ShowBoundaries, Values.TrainingControlValues.ShowPrototypes);
		}

		public bool Fullscreen {
			get { return (bool)GetValue(FullscreenProperty); }
			set { SetValue(FullscreenProperty, value); }
		}

		WindowState lastState = WindowState.Normal;
		// Using a DependencyProperty as the backing store for Fullscreen.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty FullscreenProperty =
			DependencyProperty.RegisterAttached("Fullscreen", typeof(bool), typeof(LvqWindow), new UIPropertyMetadata(false, (o, e) => {
				LvqWindow win = (LvqWindow)o;
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
			}));

		public IEnumerable<LvqModelType> ModelTypes { get { return (LvqModelType[])Enum.GetValues(typeof(LvqModelType)); } }
		public IEnumerable<long> Iters { get { return new[] { 100000L, 1000000L, 10000000L, }; } }

		void LrSearch_Click(object sender, RoutedEventArgs e) {
			uint offset = uint.Parse(rngOffsetTextBox.Text);
			LvqModelType modeltype = (LvqModelType)modelType.SelectedItem;
			int protos = Use5Protos.IsChecked == true ? 5 : 1;
			long iterCount = (long)iterCountSelectbox.SelectedItem;
			var testLr = new TestLr(iterCount, offset);
			var settings = new LvqModelSettingsCli().WithTestingChanges(modeltype, protos, testLr.offset);
			string shortname = testLr.ShortnameFor(settings);

			var logWindow = LogControl.ShowNewLogWindow(shortname, ActualWidth, ActualHeight * 0.6);
			ThreadPool.QueueUserWorkItem(_ => testLr.TestLrIfNecessary(logWindow.Item2.Writer, settings)
									.ContinueWith(t => {
										logWindow.Item1.Dispatcher.BeginInvoke(() => logWindow.Item1.Background = Brushes.White);
										t.Wait();
									}));
		}

		void LrSearchAll_Click(object sender, RoutedEventArgs e) {
			uint offset = uint.Parse(rngOffsetTextBox.Text);
			long iterCount = (long)iterCountSelectbox.SelectedItem;
			Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;
			ThreadPool.QueueUserWorkItem(_ =>
				new TestLr(iterCount, offset).StartAllLrTesting(new LvqModelSettingsCli { NgUpdateProtos = true })
				.ContinueWith(t => { Console.WriteLine("wheee!!!!"); t.Wait(); })
				);
		}

		public Task SaveAllGraphs() {
			var selectedModel = Values.TrainingControlValues.SelectedLvqModel;
			var allmodels = Values.TrainingControlValues.MatchingLvqModels.ToArray();
			var graphSettings = new { Values.TrainingControlValues.ShowBoundaries, Values.TrainingControlValues.ShowPrototypes, Values.TrainingControlValues.CurrProjStats };
			lvqPlotContainer = lvqPlotContainer ?? new LvqStatPlotsContainer(ClosingToken);
			TaskCompletionSource<object> done = new TaskCompletionSource<object>();
			done.SetResult(null);
			Task doneTask = done.Task;

			return
				allmodels.Aggregate(doneTask, (task, model) =>
					task.ContinueWith(_ => lvqPlotContainer.DisplayModel(model.InitSet, model, model.SelectedSubModel, true, graphSettings.ShowBoundaries, graphSettings.ShowPrototypes)
							.ContinueWith(__ => lvqPlotContainer.SaveAllGraphs()
								.ContinueWith(___ => lvqPlotContainer.DisplayModel(model.InitSet, model, model.SelectedSubModel, false, graphSettings.ShowBoundaries, graphSettings.ShowPrototypes)
									.ContinueWith(____ => lvqPlotContainer.SaveAllGraphs().Wait()).Wait()
								).Wait()
							).Wait()
						)
				).ContinueWith(_ => lvqPlotContainer.DisplayModel(selectedModel.InitSet, selectedModel, selectedModel.SelectedSubModel, graphSettings.CurrProjStats, graphSettings.ShowBoundaries, graphSettings.ShowPrototypes).Wait());
		}
	}
}
