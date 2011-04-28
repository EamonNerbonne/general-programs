﻿// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using EmnExtensions.Wpf;
using LvqLibCli;
using System;
using System.ComponentModel;

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
			if (plotData != null) {
				plotData.Dispose();
				plotData = null;
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
				values.CreateStarDatasetValues.Seed = 1337;
				values.CreateStarDatasetValues.InstSeed = 37;

				values.CreateLvqModelValues.Seed = 42;
				values.CreateLvqModelValues.InstSeed = 1234;

				values.CreateStarDatasetValues.ConfirmCreation();
				Dispatcher.BeginInvokeBackground(() => {
					values.CreateLvqModelValues.ConfirmCreation();
					Dispatcher.BeginInvokeBackground(() => {
						values.TrainingControlValues.AnimateTraining = true;
					});
				});
			}, DataContext);
		}

		LvqStatPlotsContainer plotData;
		void TrainingControlValues_SelectedModelUpdatedInBackgroundThread() {
			LvqStatPlotsContainer.QueueUpdateIfCurrent(plotData);
		}

		void TrainingControlValues_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "ShowBoundaries" && plotData != null)
				plotData.ShowBoundaries(Values.TrainingControlValues.ShowBoundaries);
			if (e.PropertyName == "ShowPrototypes" && plotData != null)
				plotData.ShowPrototypes(Values.TrainingControlValues.ShowPrototypes);
			if (e.PropertyName == "CurrProjStats" && plotData != null)
				plotData.ShowCurrentProjectionStats(Values.TrainingControlValues.CurrProjStats);
			if (e.PropertyName == "SelectedDataset" || e.PropertyName == "SelectedLvqModel" || e.PropertyName == "SubModelIndex")
				ModelChanged();
		}


		void ModelChanged() {
			if (plotData == null && Values.TrainingControlValues.SelectedDataset != null && Values.TrainingControlValues.SelectedLvqModel != null)
				plotData = new LvqStatPlotsContainer(ClosingToken);

			if (plotData != null)
				plotData.DisplayModel(Values.TrainingControlValues.SelectedDataset, Values.TrainingControlValues.SelectedLvqModel, Values.TrainingControlValues.SubModelIndex, Values.TrainingControlValues.CurrProjStats, Values.TrainingControlValues.ShowBoundaries, Values.TrainingControlValues.ShowPrototypes);
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
			LvqModelType modeltype = (LvqModelType)modelType.SelectedItem;
			int protos = Use5Protos.IsChecked == true ? 5 : 1;
			long iterCount = (long)iterCountSelectbox.SelectedItem;
			LrSearch(modeltype, protos, iterCount);
		}
		void LrSearchAll_Click(object sender, RoutedEventArgs e) {
			foreach (LvqModelType modeltype in ModelTypes)
				foreach (int protos in new[] { 5, 1 })
					foreach (long iterCount in Iters)
						LrSearch(modeltype, protos, iterCount, true);
		}

		private void LrSearch(LvqModelType modeltype, int protos, long iterCount, bool autoclose = false) {
			string shortname = modeltype.ToString().Replace("ModelType", "").ToLowerInvariant() + protos + "e" + (int)(Math.Log10(iterCount) + 0.5);

			var logWindow = LogControl.ShowNewLogWindow(shortname, ActualWidth, ActualHeight * 0.6);

			ThreadPool.QueueUserWorkItem(_ => {
				TestLr.Run(logWindow.Item2.Writer, modeltype, protos, iterCount);
				string contents = (string)logWindow.Item1.Dispatcher.Invoke((Func<string>)logWindow.Item2.GetContentsUI);
				TestLr.SaveLogFor(shortname, contents);
				if (autoclose)
					logWindow.Item1.Dispatcher.BeginInvoke(() => logWindow.Item1.Close());
				else
					logWindow.Item1.Dispatcher.BeginInvoke(() => logWindow.Item1.Background = Brushes.White);
			});
		}
	}
}