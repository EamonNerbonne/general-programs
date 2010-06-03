using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LvqLibCli;
using EmnExtensions.Wpf.Plot;
using EmnExtensions.Wpf.Plot.VizEngines;

namespace LvqGui {
	/// <summary>
	/// Interaction logic for LvqWindow.xaml
	/// </summary>
	public partial class LvqWindow : Window {
		public LvqWindow() {
			var windowValues = new LvqWindowValues(this.Dispatcher);
			this.DataContext = windowValues;
			InitializeComponent();
			windowValues.TrainingControlValues.ModelSelected += TrainingControlValues_ModelSelected;
			windowValues.TrainingControlValues.SelectedModelUpdatedInBackgroundThread += TrainingControlValues_SelectedModelUpdatedInBackgroundThread;
		}

		LvqScatterPlot plotData;
		void TrainingControlValues_SelectedModelUpdatedInBackgroundThread(LvqDatasetCli dataset, LvqModelCli model) {
			Dispatcher.BeginInvoke(() => {
				if (plotData != null && plotData.dataset == dataset && plotData.LvqModel == model)
					plotData.QueueUpdate();
				if (trainingStatWindow != null && trainingStatWindow.IsLoaded && model != null) {
					var trainErrData = model.TrainingStats.Select(stat => new Point(stat.trainingIter, stat.trainingError)).ToArray();
					var trainCostData = model.TrainingStats.Select(stat => new Point(stat.trainingIter, stat.trainingCost)).ToArray();
					((IPlotWriteable<Point[]>)((PlotControl)trainingStatWindow.Content).Graphs[0]).Data = trainErrData;
					((IPlotWriteable<Point[]>)((PlotControl)trainingStatWindow.Content).Graphs[1]).Data = trainCostData;
				}
			});
		}

		Window trainingStatWindow;
		void TrainingControlValues_ModelSelected(LvqDatasetCli dataset, LvqModelCli model) {
			if (plotData == null || plotData.dataset != dataset) {
				plotData = new LvqScatterPlot(dataset, Dispatcher, ((LvqWindowValues)DataContext).TrainingControlValues);
				plotControl.Graphs.Clear();
				foreach (var subplot in plotData.Plots)
					plotControl.Graphs.Add(subplot);
			}
			plotData.LvqModel = model;

			if (trainingStatWindow == null || !trainingStatWindow.IsLoaded) {
				trainingStatWindow = new Window();
				trainingStatWindow.Width = this.ActualWidth * 0.5;
				trainingStatWindow.Height = this.ActualHeight * 0.8;
				trainingStatWindow.Title = "Training statistics";
				trainingStatWindow.Show();
			}

			if (model != null) {
				var statPlots = new PlotControl();
				statPlots.ShowGridLines = true;
				var trainErr = PlotData.Create(model.TrainingStats.Select(stat => new Point(stat.trainingIter, stat.trainingError)).ToArray());
				trainErr.PlotClass = PlotClass.Line;
				trainErr.DataLabel = "Training error-rate";
				trainErr.RenderColor = Colors.Red;
				trainErr.XUnitLabel = "Training iterations";
				trainErr.YUnitLabel = "Training error-rate";
				trainErr.AxisBindings = TickedAxisLocation.BelowGraph | TickedAxisLocation.RightOfGraph;
				((IVizLineSegments)trainErr.Visualizer).CoverageRatioY = 0.98;

				var trainCost = PlotData.Create(model.TrainingStats.Select(stat => new Point(stat.trainingIter, stat.trainingCost)).ToArray());
				trainCost.PlotClass = PlotClass.Line;
				trainCost.DataLabel = "Training cost-function";
				trainCost.RenderColor = Colors.Blue;
				trainCost.XUnitLabel = "Training iterations";
				trainCost.YUnitLabel = "Training cost-function";
				((IVizLineSegments)trainCost.Visualizer).CoverageRatioY = 0.98;

				statPlots.Graphs.Add(trainErr);
				statPlots.Graphs.Add(trainCost);

				trainingStatWindow.Content = statPlots;
			}
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
