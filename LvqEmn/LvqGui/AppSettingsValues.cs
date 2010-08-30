using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using EmnExtensions.MathHelpers;

namespace LvqGui {

	public class AppSettingsValues : INotifyPropertyChanged {
		readonly LvqWindowValues owner;

		public event PropertyChangedEventHandler PropertyChanged;
		private void _propertyChanged(String propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }

		public bool ShowGridLines {
			get { return _ShowGridLines; }
			set { if (!object.Equals(_ShowGridLines, value)) { _ShowGridLines = value; _propertyChanged("ShowGridLines"); } }
		}
		private bool _ShowGridLines;

		public AppSettingsValues(LvqWindowValues owner) {
			this.owner = owner;
		}
	}
}
