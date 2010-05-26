using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace LvqGui {

	public partial class CreateLvqModel : UserControl {
		public ModelType[] ModelTypes { get { return (ModelType[])Enum.GetValues(typeof(ModelType)); } }
		public CreateLvqModel() { InitializeComponent(); }

		private void Button_Click(object sender, RoutedEventArgs e) {
			((IHasSeed)DataContext).Reseed();
		}

		public event Action CreateLvqModelConfirmed;

		private void Button_Click_1(object sender, RoutedEventArgs e) {
			if (CreateLvqModelConfirmed != null) CreateLvqModelConfirmed();
			ThreadPool.QueueUserWorkItem(o => {
				((CreateLvqModelValues)o).ConfirmCreation();
			},DataContext);
		}
	}
}
