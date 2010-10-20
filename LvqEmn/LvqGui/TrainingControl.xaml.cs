using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace LvqGui {
	/// <summary>
	/// Interaction logic for TrainingControl.xaml
	/// </summary>
	public partial class TrainingControl : UserControl {
		public TrainingControl() { InitializeComponent(); }
		private TrainingControlValues Values { get { return (TrainingControlValues)DataContext; } }

		private void StartTraining(object sender, RoutedEventArgs e) {
			ThreadPool.QueueUserWorkItem(o => { ((TrainingControlValues)o).ConfirmTraining(); }, DataContext);
		}
		private void ResetLearningRate(object sender, RoutedEventArgs e) {
			ThreadPool.QueueUserWorkItem(o => { ((TrainingControlValues)o).ResetLearningRate(); }, DataContext);
		}

		private void UnloadModel(object sender, RoutedEventArgs e) { Values.UnloadModel(); }

		private void UnloadDataset(object sender, RoutedEventArgs e) { Values.UnloadDataset(); }

		private void DoGC(object sender, RoutedEventArgs e) {
			GC.Collect();
		}
	}
}
