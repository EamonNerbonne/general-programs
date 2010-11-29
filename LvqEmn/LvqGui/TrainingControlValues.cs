// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using EmnExtensions.DebugTools;
using LvqLibCli;
using System.Threading.Tasks;
using EmnExtensions;

namespace LvqGui {
	public class TrainingControlValues : INotifyPropertyChanged {
		readonly LvqWindowValues owner;
		public LvqWindowValues Owner { get { return owner; } }

		public event PropertyChangedEventHandler PropertyChanged;
		public event Action<LvqDatasetCli, LvqModels, int> ModelSelected;
		public event Action<LvqDatasetCli, LvqModels> SelectedModelUpdatedInBackgroundThread;

		private void _propertyChanged(string propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }

		public LvqDatasetCli SelectedDataset {
			get { return _SelectedDataset; }
			set { if (!Equals(_SelectedDataset, value)) { _SelectedDataset = value; _propertyChanged("SelectedDataset"); _propertyChanged("MatchingLvqModels"); SelectedLvqModel = _SelectedDataset == null ? null : Owner.ResolveModel(_SelectedDataset.LastModel); AnimateTraining = false; } }
		}
		private LvqDatasetCli _SelectedDataset;

		public IEnumerable<LvqModels> MatchingLvqModels { get { return Owner.LvqModels.Where(model => model == null || model.FitsDataShape(SelectedDataset)); } }

		public LvqModels SelectedLvqModel {
			get { return _SelectedLvqModel; }
			set { if (!Equals(_SelectedLvqModel, value)) { _SelectedLvqModel = value; _propertyChanged("SelectedLvqModel"); _propertyChanged("ModelIndexes"); ModelSelected(_SelectedDataset, _SelectedLvqModel, _SubModelIndex); AnimateTraining = false; SubModelIndex = 0; } }
		}
		private LvqModels _SelectedLvqModel;

		public IEnumerable<int> ModelIndexes { get { return Enumerable.Range(0, SelectedLvqModel == null ? 0 : SelectedLvqModel.ModelCount); } }

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
			set { if (value < 1) throw new ArgumentException("Must train for at least 1 epoch at a  time"); if (!Equals(_EpochsPerClick, value)) { _EpochsPerClick = value; _propertyChanged("EpochsPerClick"); } }
		}
		private int _EpochsPerClick;

		public int EpochsPerAnimation {
			get { return _EpochsPerAnimation; }
			set { if (value < 1) throw new ArgumentException("Must train for at least 1 epoch at a  time"); if (!_EpochsPerAnimation.Equals(value)) { _EpochsPerAnimation = value; _propertyChanged("EpochsPerAnimation"); } }
		}
		private int _EpochsPerAnimation;

		public TrainingControlValues(LvqWindowValues owner) {
			this.owner = owner;
			EpochsPerClick = 400;
			EpochsPerAnimation = 5;
			owner.LvqModels.CollectionChanged += LvqModels_CollectionChanged;
		}

		public bool AnimateTraining {
			get { return _AnimateTraining; }
			set {
				if (!Equals(_AnimateTraining, value)) {
					if (value && (SelectedLvqModel == null || SelectedDataset == null))
						throw new ArgumentException("Can't animate; dataset or model is not set");
					_AnimateTraining = value; _propertyChanged("AnimateTraining");
					if (_AnimateTraining)
						new Thread(o => DoAnimatedTraining()) {
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
				//lock (selectedModel.UpdateSyncObject) //not needed for safety, just for accurate timing
				using (new DTimer("Training " + epochsToTrainFor + " epochs"))
					selectedModel.Train( epochsToTrainFor,  selectedDataset, Owner.WindowClosingToken);
				if (!Owner.WindowClosingToken.IsCancellationRequested) {
					PrintModelTimings(selectedModel);
					PotentialUpdate(selectedDataset, selectedModel);
				}
			}
		}

		bool isAnimating;
		public void DoAnimatedTraining() {
			if (isAnimating) return;
#if BENCHMARK
			int epochsTrained = 0;
#endif
			var selectedDataset = SelectedDataset;
			var selectedModel = SelectedLvqModel;
			try {
				isAnimating = true;
				Queue<Task> overallTask = new Queue<Task>();
				while (_AnimateTraining && !Owner.WindowClosingToken.IsCancellationRequested) {
					int epochsToTrainFor = EpochsPerAnimation;
					if (selectedDataset == null || selectedModel == null || epochsToTrainFor < 1) {
						owner.Dispatcher.BeginInvoke(() => { AnimateTraining = false; });
						break;
					}
					overallTask.Enqueue(Task.Factory.StartNew(() => {
						selectedModel.Train(epochsToTrainFor,  selectedDataset, Owner.WindowClosingToken);
						PotentialUpdate(selectedDataset, selectedModel);
					}, Owner.WindowClosingToken,TaskCreationOptions.None,LowPriorityTaskScheduler.Instance));

					if (overallTask.Count >= 2) overallTask.Dequeue().Wait();

#if BENCHMARK
					epochsTrained += epochsToTrainFor;
					if (epochsTrained >= 800) {
						_AnimateTraining = false;
						Task.WaitAll(overallTask.ToArray());
						owner.Dispatcher.BeginInvokeBackground(() => Application.Current.MainWindow.Close());
					}
#endif
				}
				try {
					Task.WaitAll(overallTask.ToArray());
				} catch (AggregateException ae) {
					if (!ae.InnerExceptions.All(e => e is OperationCanceledException))
						throw;
				}
			} finally {
				isAnimating = false;
				PrintModelTimings(selectedModel);
			}
		}

		static void PrintModelTimings(LvqModels model) {
			var trainingStats = model.TrainingStats;
			if (trainingStats.Length >= 2) {
				var lastStat = trainingStats[trainingStats.Length - 1];
				Console.WriteLine("Avg cpu seconds per iter: {0}{1}",
					lastStat.Value[LvqTrainingStatCli.ElapsedSecondsI] / lastStat.Value[LvqTrainingStatCli.TrainingIterationI],
					lastStat.StandardError != null
						? " ~ " + (lastStat.StandardError[LvqTrainingStatCli.ElapsedSecondsI] / lastStat.Value[LvqTrainingStatCli.TrainingIterationI])
						: "");
			}
		}

		private void PotentialUpdate(LvqDatasetCli selectedDataset, LvqModels selectedModel) {
			if (selectedModel == SelectedLvqModel && selectedDataset == SelectedDataset && SelectedModelUpdatedInBackgroundThread != null)
				SelectedModelUpdatedInBackgroundThread(selectedDataset, selectedModel);
		}

		public void ResetLearningRate() {
			var selectedModel = SelectedLvqModel;
			if (selectedModel != null) selectedModel.ResetLearningRate();
		}

		internal void UnloadModel() {
			var selectedModel = SelectedLvqModel;
			if (selectedModel != null) {
				SelectedLvqModel = null;
				Owner.LvqModels.Remove(selectedModel);
			}
		}

		internal void UnloadDataset() {
			var selectedDataset = SelectedDataset;
			if (selectedDataset != null) {
				SelectedDataset = null;
				Owner.Datasets.Remove(selectedDataset);
			}
		}

		internal double GetLearningRate() {
			var selectedModel = SelectedLvqModel;
			return selectedModel != null ? selectedModel.CurrentLearningRate : 0.0;
		}


	}
}
