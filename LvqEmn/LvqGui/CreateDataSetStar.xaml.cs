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
using System.ComponentModel;
using EmnExtensions.MathHelpers;
using LvqLibCli;
using System.Threading;

namespace LvqGui {
	/// <summary>
	/// Interaction logic for CreateDataSetStar.xaml
	/// </summary>
	public partial class CreateDataSetStar : UserControl {
		public CreateDataSetStar() { InitializeComponent(); }

		private void ReseedParam(object sender, RoutedEventArgs e) { ((IHasSeed)DataContext).ReseedParam(); }
		private void ReseedInst(object sender, RoutedEventArgs e) { ((IHasSeed)DataContext).ReseedParam(); }

		private void buttonGenerateDataset_Click(object sender, RoutedEventArgs e) {
			ThreadPool.QueueUserWorkItem(o => { ((CreateDataSetStarValues)o).ConfirmCreation(); }, DataContext);
		}
	}
}
