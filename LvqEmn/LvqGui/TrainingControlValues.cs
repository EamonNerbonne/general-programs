using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using LvqLibCli;
using EmnExtensions.DebugTools;
using System.Threading;
using System.Collections.ObjectModel;
using System.Windows;

namespace LvqGui {
	public class TrainingControlValues : INotifyPropertyChanged {
		readonly LvqWindowValues owner;
		public LvqWindowValues Owner { get { return owner; } }

		public event PropertyChangedEventHandler PropertyChanged;
		public event Action<LvqDatasetCli, LvqModelCli,int> ModelSelected;
		public event Action<LvqDatasetCli, LvqModelCli> SelectedModelUpdatedInBackgroundThread;

		private void _propertyChanged(string propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }

		public LvqDatasetCli SelectedDataset {
			get { return _SelectedDataset; }
			set { if (!object.Equals(_SelectedDataset, value)) { _SelectedDataset = value; _propertyChanged("SelectedDataset"); _propertyChanged("MatchingLvqModels"); SelectedLvqModel = _SelectedDataset.LastModel; AnimateTraining = false; } }
		}
		private LvqDatasetCli _SelectedDataset;

		//ObservableCollection<LvqModelCli>
		public IEnumerable<LvqModelCli> MatchingLvqModels { get { return Owner.LvqModels.Where(model => model == null || model.FitsDataShape(SelectedDataset)); } }

		public LvqModelCli SelectedLvqModel {
			get { return _SelectedLvqModel; }
			set { if (!object.Equals(_SelectedLvqModel, value)) { _SelectedLvqModel = value; _propertyChanged("SelectedLvqModel"); _propertyChanged("ModelIndexes"); ModelSelected(_SelectedDataset, _SelectedLvqModel, _SubModelIndex); AnimateTraining = false; SubModelIndex = 0; } }
		}
		private LvqModelCli _SelectedLvqModel;

		//ObservableCollection<LvqModelCli>
		public IEnumerable<int> ModelIndexes { get { return Enumerable.Range(0,SelectedLvqModel == null?0:SelectedLvqModel.ModelCount) ; } }

		public int SubModelIndex {
			get { return _SubModelIndex; }
			set {
				if (SelectedLvqModel != null && (value < 0 || value >= SelectedLvqModel.ModelCount))
					throw new ArgumentException("Model only has " + SelectedLvqModel.ModelCount + " sub-models.");
				if (!_SubModelIndex.Equals(value)) { _SubModelIndex = value; ModelSelected(_SelectedDataset, _SelectedLvqModel, _SubModelIndex); _propertyChanged("SubModelIndex"); }
			}
		}
		private int _SubModelIndex;

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
						new Thread(o => { DoAnimatedTraining(); }) {
							IsBackground = true,
							Priority = ThreadPriority.BelowNormal
						}.Start();
				}
			}
		}
		private bool _AnimateTraining;

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
				lock (selectedModel.UpdateSyncObject) //not needed for safety, just for accurate timing
					using (new DTimer("Training " + epochsToTrainFor + " epochs"))
						selectedModel.Train(epochsToDo: epochsToTrainFor, trainingSet: selectedDataset);
				PrintModelTimings(selectedModel);
				PotentialUpdate(selectedDataset, selectedModel);
			}
		}

		bool isAnimating;
		public void DoAnimatedTraining() {
			if (isAnimating) return;
			double totalTime = 0.0;
			int epochsTrained = 0;
			var selectedDataset = SelectedDataset;
			var selectedModel = SelectedLvqModel;
			try {
				isAnimating = true;
				while (_AnimateTraining) {
					int epochsToTrainFor = EpochsPerClick;
					if (selectedDataset == null || selectedModel == null || epochsToTrainFor < 1) {
						owner.Dispatcher.BeginInvoke(() => { AnimateTraining = false; });
						break;
					}

					lock (selectedModel.UpdateSyncObject) //not needed for thread safety, just for accurate timing
						using (new DTimer(ts => { totalTime += ts.TotalSeconds; epochsTrained += epochsToTrainFor; }))
							selectedModel.Train(epochsToDo: epochsToTrainFor, trainingSet: selectedDataset);
					PotentialUpdate(selectedDataset, selectedModel);
#if BENCHMARK
					if (epochsTrained >= 100) owner.Dispatcher.BeginInvokeBackground(() => { Application.Current.Shutdown(); });
#endif
				}
			} finally {
				isAnimating = false;
				Console.WriteLine("Overall took {0}s per epoch for {1} epochs", totalTime / epochsTrained, epochsTrained);
				PrintModelTimings(selectedModel);
			}
		}

		static void PrintModelTimings(LvqModelCli model) {
			var trainingStats = model.TrainingStats.ToArray();
			if (trainingStats.Length >= 2) {
				var lastStat = trainingStats[trainingStats.Length - 1];
				Console.WriteLine("Avg cpu seconds per iter: {0}{1}",
					lastStat.values[LvqTrainingStatCli.ElapsedSecondsI] / lastStat.values[LvqTrainingStatCli.TrainingIterationI],
					lastStat.stderror != null
						? " ~ " + (lastStat.stderror[LvqTrainingStatCli.ElapsedSecondsI] / lastStat.values[LvqTrainingStatCli.TrainingIterationI])
						: "");
			}
		}

		private void PotentialUpdate(LvqDatasetCli selectedDataset, LvqModelCli selectedModel) {
			if (selectedModel == SelectedLvqModel && selectedDataset == SelectedDataset && SelectedModelUpdatedInBackgroundThread != null)
				SelectedModelUpdatedInBackgroundThread(selectedDataset, selectedModel);
		}

		public void ResetLearningRate() {
			var selectedModel = SelectedLvqModel;
			if (selectedModel != null) selectedModel.ResetLearningRate();
		}
	}
}
