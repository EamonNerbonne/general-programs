using System.Linq;
using System.Windows;
using System.Threading;

namespace LvqGui {
	public partial class CreateGaussianCloudDataset {
		public CreateGaussianCloudDataset() {
			InitializeComponent();
		}

		private void ReseedParam(object sender, RoutedEventArgs e) { ((IHasSeed)DataContext).ReseedParam(); }
		private void ReseedInst(object sender, RoutedEventArgs e) { ((IHasSeed)DataContext).ReseedInst(); }

		private void CreateDatasetButtonPress(object sender, RoutedEventArgs e) {
			ThreadPool.QueueUserWorkItem(o => ((CreateGaussianCloudsDatasetValues)o).ConfirmCreation(), DataContext);
		}
	}
}
