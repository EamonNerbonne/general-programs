using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace LvqGui {

	public partial class CreateLvqModel : UserControl {
		public ModelType[] ModelTypes { get { return (ModelType[])Enum.GetValues(typeof(ModelType)); } }
		public CreateLvqModel() { InitializeComponent(); }

		private void ReseedParam(object sender, RoutedEventArgs e) { ((IHasSeed)DataContext).ReseedParam(); }
		private void ReseedInst(object sender, RoutedEventArgs e) { ((IHasSeed)DataContext).ReseedParam(); }

		private void InitializeModel(object sender, RoutedEventArgs e) {
			ThreadPool.QueueUserWorkItem(o => { ((CreateLvqModelValues)o).ConfirmCreation(); }, DataContext);
		}
	}
}
