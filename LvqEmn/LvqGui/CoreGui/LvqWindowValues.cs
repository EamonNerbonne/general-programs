// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using LvqLibCli;

namespace LvqGui {
	public class LvqWindowValues : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		void _propertyChanged(String propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }

		//public AppSettingsValues AppSettingsValues { get; private set; }
		public CreateGaussianCloudsDatasetValues CreateGaussianCloudsDatasetValues { get; private set; }
		public CreateStarDatasetValues CreateStarDatasetValues { get; private set; }
		public CreateLvqModelValues CreateLvqModelValues { get; private set; }
		public TrainingControlValues TrainingControlValues { get; private set; }
		public LoadDatasetValues LoadDatasetValues { get; private set; }

		public bool ExtendDataByCorrelation {
			get { return _ExtendDataByCorrelation; }
			set { if (!_ExtendDataByCorrelation.Equals(value)) { _ExtendDataByCorrelation = value; _propertyChanged("ExtendDataByCorrelation"); } }
		}
		bool _ExtendDataByCorrelation;

		public bool NormalizeDimensions {
			get { return _NormalizeDimensions; }
			set { if (!_NormalizeDimensions.Equals(value)) { _NormalizeDimensions = value; _propertyChanged("NormalizeDimensions"); } }
		}
		private bool _NormalizeDimensions;

		

		public ObservableCollection<LvqDatasetCli> Datasets { get; private set; }
		public ObservableCollection<LvqMultiModel> LvqModels { get; private set; }

		public CancellationToken WindowClosingToken { get { return win.ClosingToken; } }
		public Dispatcher Dispatcher { get { return win.Dispatcher; } }
		public readonly LvqWindow win;

		public LvqWindowValues(LvqWindow win) {
			if (win == null) throw new ArgumentNullException("win");
			this.win = win;
			Datasets = new ObservableCollection<LvqDatasetCli>();
			LvqModels = new ObservableCollection<LvqMultiModel>();

			//AppSettingsValues = new AppSettingsValues(this);
			CreateGaussianCloudsDatasetValues = new CreateGaussianCloudsDatasetValues(this);
			CreateStarDatasetValues = new CreateStarDatasetValues(this);
			CreateLvqModelValues = new CreateLvqModelValues(this);
			TrainingControlValues = new TrainingControlValues(this);
			LoadDatasetValues = new LoadDatasetValues(this);

			Datasets.CollectionChanged += Datasets_CollectionChanged;
			LvqModels.CollectionChanged += LvqModels_CollectionChanged;
		}

		void LvqModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (e.NewItems != null && e.NewItems.Count > 0) {
				foreach (LvqMultiModel modelGroup in e.NewItems)
					foreach (LvqModelCli subModel in modelGroup.SubModels)
						modelGroupLookup.Add(subModel, modelGroup);
				var newModelGroup = e.NewItems.Cast<LvqMultiModel>().First();
				TrainingControlValues.SelectedDataset = newModelGroup.InitSet;
				TrainingControlValues.SelectedLvqModel = newModelGroup;
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
			if (e.OldItems != null)
				foreach (LvqMultiModel modelGroup in e.OldItems)
					foreach (LvqModelCli subModel in modelGroup.SubModels)
						if (!modelGroupLookup.Remove(subModel))
							throw new InvalidOperationException("How can you be removing models that aren't in the lookup... ehh....?");

		}

		readonly Dictionary<LvqModelCli, LvqMultiModel> modelGroupLookup = new Dictionary<LvqModelCli, LvqMultiModel>();

		public LvqMultiModel ResolveModel(LvqModelCli lastModel) {
			LvqMultiModel multiModel;
			modelGroupLookup.TryGetValue(lastModel, out multiModel);
			return multiModel;
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
