using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using LvqLibCli;

namespace LvqGui {
	public class TrainingControlValues : INotifyPropertyChanged {
		readonly LvqWindowValues owner;
		public LvqWindowValues Owner { get { return owner; } }
		
		public event PropertyChangedEventHandler PropertyChanged;

		private void _propertyChanged(String propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }

		public LvqDataSetCli SelectedDataSet {
			get { return _SelectedDataSet; }
			set { if (!object.Equals(_SelectedDataSet,value)) { _SelectedDataSet = value; _propertyChanged("SelectedDataSet"); } }
		}
		private LvqDataSetCli _SelectedDataSet;

		public LvqModelCli SelectedLvqModel {
			get { return _SelectedLvqModel; }
			set { if (!object.Equals(_SelectedLvqModel,value)) { _SelectedLvqModel = value; _propertyChanged("SelectedLvqModel"); } }
		}
		private LvqModelCli _SelectedLvqModel;

		public int EpochsPerClick {
			get { return _EpochsPerClick; }
			set { if (value < 1) throw new ArgumentException("Must train for at least 1 epoch at a  time"); if (!object.Equals(_EpochsPerClick,value)) { _EpochsPerClick = value; _propertyChanged("EpochsPerClick"); } }
		}
		private int _EpochsPerClick;


		public TrainingControlValues(LvqWindowValues owner) {
			this.owner = owner;
			EpochsPerClick = 1;
		}

		public void ConfirmTraining() {
			Console.WriteLine("supposedly training");
		}
	}
}
