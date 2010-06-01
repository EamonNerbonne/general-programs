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
			});
		}

		void TrainingControlValues_ModelSelected(LvqDatasetCli dataset, LvqModelCli model) {
			if (plotData == null || plotData.dataset != dataset) {
				plotData = new LvqScatterPlot(dataset, Dispatcher,((LvqWindowValues)DataContext).TrainingControlValues);
				plotControl.Graphs.Clear();
				foreach (var subplot in plotData.Plots)
					plotControl.Graphs.Add(subplot);
			}
			plotData.LvqModel = model;
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
