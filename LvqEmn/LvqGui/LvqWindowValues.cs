using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LvqLibCli;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Threading;

namespace LvqGui {
	public class LvqWindowValues : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		private void _propertyChanged(String propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }

		//public AppSettingsValues AppSettingsValues { get; private set; }
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

		public readonly Dispatcher Dispatcher;
		public readonly LvqWindow win;

		public LvqWindowValues(LvqWindow win) {
			if (win == null) throw new ArgumentNullException("win");
			this.win = win;
			this.Dispatcher = win.Dispatcher;
			Datasets = new ObservableCollection<LvqDatasetCli>();
			LvqModels = new ObservableCollection<LvqModelCli>();

			//AppSettingsValues = new AppSettingsValues(this);
			CreateDatasetValues = new CreateDatasetValues(this);
			CreateDatasetStarValues = new CreateDatasetStarValues(this);
			CreateLvqModelValues = new CreateLvqModelValues(this);
			TrainingControlValues = new TrainingControlValues(this);
			LoadDatasetValues = new LoadDatasetValues(this);

			Datasets.CollectionChanged += Datasets_CollectionChanged;
			LvqModels.CollectionChanged += LvqModels_CollectionChanged;
		}

		void LvqModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (e.NewItems != null && e.NewItems.Count > 0) {
				var newModel = e.NewItems.Cast<LvqModelCli>().First();
				TrainingControlValues.SelectedDataset = newModel.InitSet;
				TrainingControlValues.SelectedLvqModel = newModel;
				win.trainingTab.IsSelected = true;
			}
			if (e.OldItems != null && e.OldItems.Contains(TrainingControlValues.SelectedLvqModel)) {
				var newModel = LvqModels.LastOrDefault();
				if (newModel == null) {
					TrainingControlValues.SelectedLvqModel = null;
				} else {
					TrainingControlValues.SelectedDataset = newModel.InitSet;
					TrainingControlValues.SelectedLvqModel = newModel;
				}
			}
		}

		void Datasets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (e.NewItems != null) {
				foreach (LvqDatasetCli newDataset in e.NewItems) {
					CreateLvqModelValues.ForDataset = newDataset;
					ThreadPool.QueueUserWorkItem(o => {
						LvqDatasetCli dataset = (LvqDatasetCli)o;
						var errorRateAndVar = dataset.GetPcaNnErrorRate();
						Console.WriteLine("NN error rate under PCA: {0} ~ {1}", errorRateAndVar.Item1, Math.Sqrt(errorRateAndVar.Item2));
					}, newDataset);
				}
				win.modelTab.IsSelected = true;
			}
			if (e.OldItems != null) {
				if (e.OldItems.Contains(CreateLvqModelValues.ForDataset))
					CreateLvqModelValues.ForDataset = Datasets.LastOrDefault();
				foreach (var model in LvqModels.Where(model => e.OldItems.Contains(model.InitSet)).ToArray())
					LvqModels.Remove(model);
			}

		}
	}
}
