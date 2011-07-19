using System;
using System.Threading;
using System.Windows;

namespace LvqGui {
	public partial class TrainingControl {
		public TrainingControl() { InitializeComponent(); }
		TrainingControlValues Values { get { return (TrainingControlValues)DataContext; } }
		void StartTraining(object sender, RoutedEventArgs e) { ThreadPool.QueueUserWorkItem(o => ((TrainingControlValues)o).ConfirmTraining(), DataContext); }
		void StartPrintOrderTraining(object sender, RoutedEventArgs e) { ThreadPool.QueueUserWorkItem(o => ((TrainingControlValues)o).ConfirmTrainingPrintOrder(), DataContext); }
		void StartTrainingSortedOrder(object sender, RoutedEventArgs e) { ThreadPool.QueueUserWorkItem(o => ((TrainingControlValues)o).ConfirmTrainingSortedOrder(), DataContext); }
		void TrainUpto(object sender, RoutedEventArgs e) { ThreadPool.QueueUserWorkItem(o => ((TrainingControlValues)o).TrainUptoIters(), DataContext); }
		void TrainAllUpto(object sender, RoutedEventArgs e) { ThreadPool.QueueUserWorkItem(o => ((TrainingControlValues)o).TrainAllUptoIters(), DataContext); }
		void ResetLearningRate(object sender, RoutedEventArgs e) { ThreadPool.QueueUserWorkItem(o => ((TrainingControlValues)o).ResetLearningRate(), DataContext); }
		void UnloadModel(object sender, RoutedEventArgs e) { Values.UnloadModel(); }
		void UnloadDataset(object sender, RoutedEventArgs e) { Values.UnloadDataset(); }
		void DoGC(object sender, RoutedEventArgs e) { GC.Collect(); }
		void PrintCurrentStats(object sender, RoutedEventArgs e) { ThreadPool.QueueUserWorkItem(o => ((TrainingControlValues)o).PrintCurrentStats(), DataContext); }
		void PrintLearningRate(object sender, RoutedEventArgs e) { Console.WriteLine(Values.GetLearningRate()); }
		void SaveAllGraphs(object sender, RoutedEventArgs e) { ((TrainingControlValues)DataContext).Owner.win.SaveAllGraphs(); }

	}
}
