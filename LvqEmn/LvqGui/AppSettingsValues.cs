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

		public bool ShowGridLines {
			get { return _ShowGridLines; }
			set { if (!_ShowGridLines.Equals(value)) { _ShowGridLines = value; _propertyChanged("ShowGridLines"); } }
		}
		private bool _ShowGridLines;

		public AppSettingsValues(LvqWindowValues owner) {
			this.owner = owner;
			GlobalInstSeed = RndHelper.MakeSecureUInt();
			GlobalParamSeed = RndHelper.MakeSecureUInt();
		}
	}
}
