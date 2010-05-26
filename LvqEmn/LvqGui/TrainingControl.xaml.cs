using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace LvqGui {
	/// <summary>
	/// Interaction logic for TrainingControl.xaml
	/// </summary>
	public partial class TrainingControl : UserControl {
		public TrainingControl() { InitializeComponent(); }

		private void Button_Click(object sender, RoutedEventArgs e) {
			ThreadPool.QueueUserWorkItem(o => {
				((TrainingControlValues)o).ConfirmTraining();
			}, DataContext);
		}
	}
}
