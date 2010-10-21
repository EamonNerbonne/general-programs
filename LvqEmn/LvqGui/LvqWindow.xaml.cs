using System.Threading;
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
				values.CreateDatasetStarValues.Seed = 1337;
				values.CreateDatasetStarValues.InstSeed = 37;
				values.CreateDatasetStarValues.ClusterDimensionality = 4;
				values.CreateDatasetStarValues.Dimensions = 24;
				values.CreateDatasetStarValues.NumberOfClasses = 5;

				values.CreateLvqModelValues.Seed = 42;
				values.CreateLvqModelValues.InstSeed = 1234;
				values.CreateLvqModelValues.PrototypesPerClass = 3;

				values.CreateDatasetStarValues.ConfirmCreation();
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
			Dispatcher.BeginInvoke(() => {
				if (plotData != null && plotData.Dataset == dataset && plotData.Model == model)
					plotData.QueueUpdate();
			});
		}

		void TrainingControlValues_ModelSelected(LvqDatasetCli dataset, LvqModelCli model, int subModelIdx) {
			if (plotData == null || plotData.Dataset != dataset || plotData.Model != model) {
				//something's different
				if (plotData != null) {
					plotData.Dispose();
					plotData = null;
				}
				if (dataset != null && model != null) {
					plotData = new LvqScatterPlot(dataset, model, subModelIdx);
				}
			} else// implies (plotData != null) 
				plotData.SubModelIndex = subModelIdx;
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
