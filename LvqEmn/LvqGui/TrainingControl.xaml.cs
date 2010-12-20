using System;
using System.Threading;
using System.Windows;

namespace LvqGui {
	public partial class TrainingControl
	{
		public TrainingControl() { InitializeComponent(); }
		TrainingControlValues Values { get { return (TrainingControlValues)DataContext; } }

		void StartTraining(object sender, RoutedEventArgs e) {
			ThreadPool.QueueUserWorkItem(o => ((TrainingControlValues)o).ConfirmTraining(), DataContext);
		}
		void ResetLearningRate(object sender, RoutedEventArgs e) {
			ThreadPool.QueueUserWorkItem(o => ((TrainingControlValues)o).ResetLearningRate(), DataContext);
		}

		void UnloadModel(object sender, RoutedEventArgs e) { Values.UnloadModel(); }

		void UnloadDataset(object sender, RoutedEventArgs e) { Values.UnloadDataset(); }

		void DoGC(object sender, RoutedEventArgs e) { GC.Collect(); }

		void PrintLearningRate(object sender, RoutedEventArgs e) { Console.WriteLine(Values.GetLearningRate()); }
	}
}
