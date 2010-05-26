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
		public event Action<LvqDataSetCli, LvqModelCli> ModelSelected;

		private void _propertyChanged(String propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }

		public LvqDataSetCli SelectedDataSet {
			get { return _SelectedDataSet; }
			set { if (!object.Equals(_SelectedDataSet, value)) { _SelectedDataSet = value; _propertyChanged("SelectedDataSet"); SelectedLvqModel = _SelectedDataSet.LastModel; } }
		}
		private LvqDataSetCli _SelectedDataSet;

		public IEnumerable<LvqModelCli> MatchingLvqModels { get { return Owner.LvqModels.Where(model => model.FitsDataShape(SelectedDataSet)); } }

		public LvqModelCli SelectedLvqModel {
			get { return _SelectedLvqModel; }
			set { if (!object.Equals(_SelectedLvqModel, value)) { _SelectedLvqModel = value; _propertyChanged("SelectedLvqModel"); ModelSelected(_SelectedDataSet, _SelectedLvqModel); } }
		}
		private LvqModelCli _SelectedLvqModel;

		public int EpochsPerClick {
			get { return _EpochsPerClick; }
			set { if (value < 1) throw new ArgumentException("Must train for at least 1 epoch at a  time"); if (!object.Equals(_EpochsPerClick, value)) { _EpochsPerClick = value; _propertyChanged("EpochsPerClick"); } }
		}
		private int _EpochsPerClick;

		public TrainingControlValues(LvqWindowValues owner) {
			this.owner = owner;
			EpochsPerClick = 1;
			owner.LvqModels.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(LvqModels_CollectionChanged);
		}

		void LvqModels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
			_propertyChanged("MatchingLvqModels");
		}

		public void ConfirmTraining() {
			Console.WriteLine("supposedly training");
		}
	}
}
