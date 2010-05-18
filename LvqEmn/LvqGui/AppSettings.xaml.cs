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

namespace LvqGui {
	/// <summary>
	/// Interaction logic for AppSettings.xaml
	/// </summary>
	public partial class AppSettings : UserControl {
		public AppSettings() {
			InitializeComponent();
		}
	}

	public class AppSettingsValues : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		private void _propertyChanged(String propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }

		/// <summary>
		/// for parameters of models+datasets
		/// </summary>
		public uint GlobalParamSeed {
			get { return _GlobalParamSeed; }
			set { if (!_GlobalParamSeed.Equals(value)) { _GlobalParamSeed = value; _propertyChanged("GlobalParamSeed"); } }
		}
		private uint _GlobalParamSeed;

		/// <summary>
		/// For instances of datasets, and learning orders
		/// </summary>
		public uint GlobalInstSeed {
			get { return _GlobalInstSeed; }
			set { if (!_GlobalInstSeed.Equals(value)) { _GlobalInstSeed = value; _propertyChanged("GlobalInstSeed"); } }
		}
		private uint _GlobalInstSeed;
	}
}
