using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace LvqGui {
	/// <summary>
	/// Interaction logic for CreateStarDataset.xaml
	/// </summary>
	public partial class CreateStarDataset : UserControl {
		public CreateStarDataset() { InitializeComponent(); }

		private void ReseedParam(object sender, RoutedEventArgs e) { ((IHasSeed)DataContext).ReseedParam(); }
		private void ReseedInst(object sender, RoutedEventArgs e) { ((IHasSeed)DataContext).ReseedInst(); }

		private void buttonGenerateDataset_Click(object sender, RoutedEventArgs e) {
			ThreadPool.QueueUserWorkItem(o => { ((CreateStarDatasetValues)o).ConfirmCreation(); }, DataContext);
		}
	}
}
