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
using System.ComponentModel;

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
			errorRateWindow = MakeSubWin ("Error Rate");
			costFuncWindow = MakeSubWin("Cost Function");
			pNormWindow = MakeSubWin("Model project norms");
			extraWindow = MakeSubWin("Extra data");
			this.Closing += (o, e) => { windowValues.TrainingControlValues.AnimateTraining = false; };
		}

		static void HideNotClose(object sender, CancelEventArgs e) {
			Window win = (Window)sender;
			e.Cancel = true;
			win.Dispatcher.BeginInvoke(win.Hide);
		}

		Window MakeSubWin(string title) {
			var win= new Window {
				Width = Application.Current.MainWindow.Width * 0.5,
				Height = Application.Current.MainWindow.Height * 0.8,
				Title = title,
				Content = new PlotControl() {
					ShowGridLines = true,
				}
			};
			win.Closing += HideNotClose;
			return win;
		}

		Window errorRateWindow, costFuncWindow, pNormWindow,extraWindow;

		LvqScatterPlot plotData;
		void TrainingControlValues_SelectedModelUpdatedInBackgroundThread(LvqDatasetCli dataset, LvqModelCli model) {
			Dispatcher.BeginInvoke(() => {
				if (plotData != null && plotData.dataset == dataset && plotData.model == model)
					plotData.QueueUpdate();
			});
		}

		void TrainingControlValues_ModelSelected(LvqDatasetCli dataset, LvqModelCli model) {
			if (dataset == null || model == null) {
				pNormWindow.Hide();
				errorRateWindow.Hide();
				costFuncWindow.Hide();
				extraWindow.Hide();
			} else {
				if (plotData == null || plotData.dataset != dataset || plotData.model != model)
					plotData = new LvqScatterPlot(dataset, model, Dispatcher, plotControl, (PlotControl)errorRateWindow.Content, (PlotControl)costFuncWindow.Content,  (PlotControl)pNormWindow.Content, (PlotControl)extraWindow.Content);
				pNormWindow.Show();
				errorRateWindow.Show();
				costFuncWindow.Show();
				extraWindow.Show();
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
