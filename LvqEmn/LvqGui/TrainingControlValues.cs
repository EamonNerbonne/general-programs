using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using LvqLibCli;
using EmnExtensions.DebugTools;
using System.Threading;
using System.Collections.ObjectModel;

namespace LvqGui {
	public class TrainingControlValues : INotifyPropertyChanged {
		readonly LvqWindowValues owner;
		public LvqWindowValues Owner { get { return owner; } }

		public event PropertyChangedEventHandler PropertyChanged;
		public event Action<LvqDatasetCli, LvqModelCli> ModelSelected;
		public event Action<LvqDatasetCli, LvqModelCli> SelectedModelUpdatedInBackgroundThread;

		private void _propertyChanged(String propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }

		public LvqDatasetCli SelectedDataset {
			get { return _SelectedDataset; }
			set { if (!object.Equals(_SelectedDataset, value)) { _SelectedDataset = value; _propertyChanged("SelectedDataset"); _propertyChanged("MatchingLvqModels"); SelectedLvqModel = _SelectedDataset.LastModel; AnimateTraining = false; } }
		}
		private LvqDatasetCli _SelectedDataset;

		//ObservableCollection<LvqModelCli>
		public IEnumerable<LvqModelCli> MatchingLvqModels { get { return Owner.LvqModels.Where(model => model==null|| model.FitsDataShape(SelectedDataset)); } }

		public LvqModelCli SelectedLvqModel {
			get { return _SelectedLvqModel; }
			set { if (!object.Equals(_SelectedLvqModel, value)) { _SelectedLvqModel = value; _propertyChanged("SelectedLvqModel"); ModelSelected(_SelectedDataset, _SelectedLvqModel); AnimateTraining = false; } }
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
			owner.LvqModels.CollectionChanged += LvqModels_CollectionChanged;
		}

		public bool AnimateTraining {
			get { return _AnimateTraining; }
			set {
				if (!object.Equals(_AnimateTraining, value)) {
					if (value && (SelectedLvqModel == null || SelectedDataset == null))
						throw new ArgumentException("Can't animate; dataset or model is not set");
					_AnimateTraining = value; _propertyChanged("AnimateTraining");
					if (_AnimateTraining)
						ThreadPool.QueueUserWorkItem(o => { ConfirmTraining(); });
				}
			}
		}
		private bool _AnimateTraining;

		public void OnIdle() {
			if (!AnimateTraining) {
			} else if (SelectedLvqModel == null || SelectedDataset == null) {
				AnimateTraining = false;
			} else {
				ThreadPool.QueueUserWorkItem(o => { ConfirmTraining(); });
			}
		}

		//			Dispatcher.BeginInvoke((Action)(() => {
		//    int epochsTodoN = EpochsPerClick ?? 1;
		//    ThreadPool.QueueUserWorkItem(DoAniStep, epochsTodoN);
		//}),DispatcherPriority.ApplicationIdle);

		void LvqModels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
			_propertyChanged("MatchingLvqModels");
		}


		public void ConfirmTraining() {
			var selectedDataset = SelectedDataset;
			var selectedModel = SelectedLvqModel;
			int epochsToTrainFor = EpochsPerClick;
			if (selectedDataset == null)
				Console.WriteLine("Training aborted, no dataset selected.");
			else if (selectedModel == null)
				Console.WriteLine("Training aborted, no model selected.");
			else {
				if (_AnimateTraining)
					selectedModel.Train(epochsToDo: epochsToTrainFor, trainingSet: selectedDataset);
				else
					lock (selectedModel.UpdateSyncObject) //not needed for safety, just for accurate timing
						using (new DTimer("Training " + epochsToTrainFor + " epochs"))
							selectedModel.Train(epochsToDo: epochsToTrainFor, trainingSet: selectedDataset);
				if (selectedModel == SelectedLvqModel && selectedDataset == SelectedDataset && SelectedModelUpdatedInBackgroundThread != null)
					SelectedModelUpdatedInBackgroundThread(selectedDataset, selectedModel);
			}
		}

		public void ResetLearningRate() {
			var selectedModel = SelectedLvqModel;
			if (selectedModel != null) selectedModel.ResetLearningRate();
		}
	}
}
