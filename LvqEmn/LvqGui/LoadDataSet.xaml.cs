﻿using System;
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
	public partial class LoadDataset : UserControl {
		public LoadDataset() { InitializeComponent(); }
		private void ReseedInst(object sender, RoutedEventArgs e) { ((IHasSeed)DataContext).ReseedInst(); }

		private void ConfirmLoadDataset(object sender, RoutedEventArgs e) {
			ThreadPool.QueueUserWorkItem(o => { ((LoadDatasetValues)o).ConfirmCreation(); }, DataContext);
		}
	}
}
