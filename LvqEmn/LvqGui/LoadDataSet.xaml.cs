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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;

namespace LvqGui {
	/// <summary>
	/// Interaction logic for LoadDataSet.xaml
	/// </summary>
	public partial class LoadDataSet : UserControl {
		public LoadDataSet() { InitializeComponent(); }

		private void LoadDataset(object sender, RoutedEventArgs e) {
			ThreadPool.QueueUserWorkItem(o => { ((LoadDataSetValues)o).ConfirmCreation(); }, DataContext);
		}
	}
}
