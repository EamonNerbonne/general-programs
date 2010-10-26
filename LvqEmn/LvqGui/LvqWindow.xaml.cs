﻿using System.Threading;
using System.Windows;
using System.Windows.Threading;
using LvqLibCli;

namespace LvqGui {
	public partial class LvqWindow : Window {
		public LvqWindow() {
			Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
			var windowValues = new LvqWindowValues(this);
			this.DataContext = windowValues;
			InitializeComponent();
			windowValues.TrainingControlValues.ModelSelected += TrainingControlValues_ModelSelected;
			windowValues.TrainingControlValues.SelectedModelUpdatedInBackgroundThread += TrainingControlValues_SelectedModelUpdatedInBackgroundThread;
			this.Closing += (o, e) => { windowValues.TrainingControlValues.AnimateTraining = false; };
#if BENCHMARK
			this.Loaded += (o, e) => { DoBenchmark(); };
#endif
		}

		private void DoBenchmark() {
			ThreadPool.QueueUserWorkItem(o => {
				LvqWindowValues values = ((LvqWindowValues)o);
				values.CreateStarDatasetValues.Seed = 1337;
				values.CreateStarDatasetValues.InstSeed = 37;
				values.CreateStarDatasetValues.ClusterDimensionality = 4;
				values.CreateStarDatasetValues.Dimensions = 24;
				values.CreateStarDatasetValues.NumberOfClasses = 5;

				values.CreateLvqModelValues.Seed = 42;
				values.CreateLvqModelValues.InstSeed = 1234;
				values.CreateLvqModelValues.PrototypesPerClass = 3;

				values.CreateStarDatasetValues.ConfirmCreation();
				Dispatcher.BeginInvokeBackground(() => {
					ThreadPool.QueueUserWorkItem(o2 => {
						values.CreateLvqModelValues.ConfirmCreation();
						Dispatcher.BeginInvokeBackground(() => {
							values.TrainingControlValues.AnimateTraining = true;
						});
					});
				});
			}, DataContext);
		}

		LvqScatterPlot plotData;
		void TrainingControlValues_SelectedModelUpdatedInBackgroundThread(LvqDatasetCli dataset, LvqModelCli model) {
			LvqScatterPlot.QueueUpdateIfCurrent(plotData, dataset, model);
		}

		void TrainingControlValues_ModelSelected(LvqDatasetCli dataset, LvqModelCli model, int subModelIdx) {
			if (plotData == null && dataset != null && model != null)
				plotData = new LvqScatterPlot();

			if (plotData != null)
				plotData.DisplayModel(dataset, model, subModelIdx);
		}

		public bool Fullscreen {
			get { return (bool)GetValue(FullscreenProperty); }
			set { SetValue(FullscreenProperty, value); }
		}

		private WindowState lastState = WindowState.Normal;
		// Using a DependencyProperty as the backing store for Fullscreen.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty FullscreenProperty =
			DependencyProperty.RegisterAttached("Fullscreen", typeof(bool), typeof(LvqWindow), new UIPropertyMetadata((object)false, new PropertyChangedCallback(
				(o, e) => {
					LvqWindow win = (LvqWindow)o;
					if ((bool)e.NewValue == true) {
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
				})));
	}
}
