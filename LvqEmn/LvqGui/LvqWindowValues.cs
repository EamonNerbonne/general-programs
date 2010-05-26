using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LvqLibCli;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Threading;

namespace LvqGui {
	public class LvqWindowValues : INotifyPropertyChanged	 {
		public event PropertyChangedEventHandler PropertyChanged;
		private void _propertyChanged(String propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }

		public AppSettingsValues AppSettingsValues { get; private set; }
		public CreateDataSetValues CreateDataSetValues { get; private set; }
		public CreateDataSetStarValues CreateDataSetStarValues { get; private set; }
		public CreateLvqModelValues CreateLvqModelValues { get; private set; }
		public TrainingControlValues TrainingControlValues { get; private set; }

		public ObservableCollection<LvqDataSetCli> DataSets { get; private set; }
		public ObservableCollection<LvqModelCli> LvqModels { get; private set; }

		public LvqDataSetCli LastDataSet {
			get { return _LastDataSet; }
			set { if (!object.Equals(_LastDataSet,value)) { _LastDataSet = value; _propertyChanged("LastDataSet"); } }
		}
		private LvqDataSetCli _LastDataSet;
		public readonly Dispatcher Dispatcher;

		public LvqWindowValues(Dispatcher dispatcher) {
			if (dispatcher == null) throw new ArgumentNullException("dispatcher");
			this.Dispatcher = dispatcher;
			DataSets = new ObservableCollection<LvqDataSetCli>();
			LvqModels = new ObservableCollection<LvqModelCli>();

			AppSettingsValues = new AppSettingsValues(this);
			CreateDataSetValues = new CreateDataSetValues(this);
			CreateDataSetStarValues = new CreateDataSetStarValues(this);
			CreateLvqModelValues = new CreateLvqModelValues(this);
			TrainingControlValues = new TrainingControlValues(this);

			DataSets.CollectionChanged += DataSets_CollectionChanged;
			LvqModels.CollectionChanged += LvqModels_CollectionChanged;
		}

		void LvqModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count > 0) {
				var newModel = e.NewItems.Cast<LvqModelCli>().First();
				TrainingControlValues.SelectedDataSet = newModel.InitSet;
				TrainingControlValues.SelectedLvqModel = newModel;
			}
		}

		void DataSets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count > 0) {
				LastDataSet = e.NewItems.Cast<LvqDataSetCli>().First();
				CreateLvqModelValues.ForDataset = LastDataSet;
			}
		}
	}
}
