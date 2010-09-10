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
	public class LvqWindowValues : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		private void _propertyChanged(String propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }

		public AppSettingsValues AppSettingsValues { get; private set; }
		public CreateDatasetValues CreateDatasetValues { get; private set; }
		public CreateDatasetStarValues CreateDatasetStarValues { get; private set; }
		public CreateLvqModelValues CreateLvqModelValues { get; private set; }
		public TrainingControlValues TrainingControlValues { get; private set; }
		public LoadDatasetValues LoadDatasetValues { get; private set; }

		public bool ExtendDataByCorrelation {
			get { return _ExtendDataByCorrelation; }
			set { if (!_ExtendDataByCorrelation.Equals(value)) { _ExtendDataByCorrelation = value; _propertyChanged("ExtendDataByCorrelation"); } }
		}
		private bool _ExtendDataByCorrelation;


		public ObservableCollection<LvqDatasetCli> Datasets { get; private set; }
		public ObservableCollection<LvqModelCli> LvqModels { get; private set; }

		public LvqDatasetCli LastDataset {
			get { return _LastDataset; }
			set { if (!object.Equals(_LastDataset, value)) { _LastDataset = value; _propertyChanged("LastDataset"); } }
		}
		private LvqDatasetCli _LastDataset;
		public readonly Dispatcher Dispatcher;

		public LvqWindowValues(Dispatcher dispatcher) {
			if (dispatcher == null) throw new ArgumentNullException("dispatcher");
			this.Dispatcher = dispatcher;
			Datasets = new ObservableCollection<LvqDatasetCli>();
			LvqModels = new ObservableCollection<LvqModelCli>();

			AppSettingsValues = new AppSettingsValues(this);
			CreateDatasetValues = new CreateDatasetValues(this);
			CreateDatasetStarValues = new CreateDatasetStarValues(this);
			CreateLvqModelValues = new CreateLvqModelValues(this);
			TrainingControlValues = new TrainingControlValues(this);
			LoadDatasetValues = new LoadDatasetValues(this);

			Datasets.CollectionChanged += Datasets_CollectionChanged;
			LvqModels.CollectionChanged += LvqModels_CollectionChanged;
		}

		void LvqModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count > 0) {
				var newModel = e.NewItems.Cast<LvqModelCli>().First();
				TrainingControlValues.SelectedDataset = newModel.InitSet;
				TrainingControlValues.SelectedLvqModel = newModel;
			}
		}

		void Datasets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count > 0) {
				LastDataset = e.NewItems.Cast<LvqDatasetCli>().First();
				CreateLvqModelValues.ForDataset = LastDataset;
			}
		}

	}
}
