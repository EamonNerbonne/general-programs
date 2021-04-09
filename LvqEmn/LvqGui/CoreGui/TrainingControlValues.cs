// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using EmnExtensions.DebugTools;
using LvqLibCli;

namespace LvqGui
{
    public class TrainingControlValues : INotifyPropertyChanged
    {
        readonly LvqWindowValues owner;
        public LvqWindowValues Owner => owner;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action SelectedModelUpdatedInBackgroundThread;

        void _propertyChanged(string propertyName)
        {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public LvqDatasetCli SelectedDataset
        {
            get => _SelectedDataset;
            set {
                if (!Equals(_SelectedDataset, value)) {
                    _SelectedDataset = value;
                    SelectedLvqModel = _SelectedDataset == null ? null : Owner.ResolveModel(_SelectedDataset.LastModel);
                    AnimateTraining = false;
                    _propertyChanged("SelectedDataset");
                    _propertyChanged("ItersPerEpoch");
                    _propertyChanged("MatchingLvqModels");
                    _propertyChanged("ModelClasses");
                }
            }
        }

        LvqDatasetCli _SelectedDataset;

        public IEnumerable<LvqMultiModel> MatchingLvqModels => Owner.LvqModels.Where(model => model == null || model.InitSet == SelectedDataset); // model.FitsDataShape(SelectedDataset) is unhandy

        public double ItersPerEpoch => SelectedDataset == null ? double.NaN : LvqMultiModel.GetItersPerEpoch(SelectedDataset, 0);


        public LvqMultiModel SelectedLvqModel
        {
            get => _SelectedLvqModel;
            set {
                if (!Equals(_SelectedLvqModel, value)) {
                    _SelectedLvqModel = value;
                    _propertyChanged("SelectedLvqModel");
                    _propertyChanged("ModelIndexes");
                    AnimateTraining = false;
                    _propertyChanged("SubModelIndex");
                }
            }
        }

        LvqMultiModel _SelectedLvqModel;

        public IEnumerable<int> ModelIndexes => Enumerable.Range(0, SelectedLvqModel == null ? 0 : SelectedLvqModel.ModelCount);

        public int SubModelIndex
        {
            get => SelectedLvqModel == null ? 0 : SelectedLvqModel.SelectedSubModel;
            set {
                if (SelectedLvqModel != null && (value < 0 || value >= SelectedLvqModel.ModelCount)) {
                    throw new ArgumentException("Model only has " + SelectedLvqModel.ModelCount + " sub-models.");
                }

                if (SelectedLvqModel != null && !SelectedLvqModel.SelectedSubModel.Equals(value)) {
                    SelectedLvqModel.SelectedSubModel = value;
                    _propertyChanged("SubModelIndex");
                }
            }
        }

        public IEnumerable<StatisticsViewMode> StatisticsViewModes => (StatisticsViewMode[])Enum.GetValues(typeof(StatisticsViewMode));

        public StatisticsViewMode CurrProjStats
        {
            get => _CurrProjStats;
            set {
                if (!_CurrProjStats.Equals(value)) {
                    _CurrProjStats = value;
                    _propertyChanged("CurrProjStats");
                }
            }
        }

        StatisticsViewMode _CurrProjStats;

        public IEnumerable<object> ModelClasses
        {
            get {
                if (SelectedDataset == null) {
                    return Array.Empty<object>();
                }

                return SelectedDataset.ClassColors.Zip(SelectedDataset.ClassNames, (col, name) => new { ClassLabel = name, ClassColor = (SolidColorBrush)new SolidColorBrush(col).GetAsFrozen() }).ToArray();
            }
        }

        public bool ShowBoundaries
        {
            get => _ShowBoundaries;
            set {
                if (!_ShowBoundaries.Equals(value)) {
                    _ShowBoundaries = value;
                    _propertyChanged("ShowBoundaries");
                }
            }
        }

        bool _ShowBoundaries;

        public bool ShowPrototypes
        {
            get => _ShowPrototypes;
            set {
                if (!_ShowPrototypes.Equals(value)) {
                    _ShowPrototypes = value;
                    _propertyChanged("ShowPrototypes");
                }
            }
        }

        bool _ShowPrototypes;

        public bool ShowTestEmbedding
        {
            get => _ShowTestEmbedding;
            set {
                if (!_ShowTestEmbedding.Equals(value)) {
                    _ShowTestEmbedding = value;
                    _propertyChanged("ShowTestEmbedding");
                }
            }
        }

        bool _ShowTestEmbedding;

        public bool ShowTestErrorRates
        {
            get => _ShowTestErrorRates;
            set {
                if (!_ShowTestErrorRates.Equals(value)) {
                    _ShowTestErrorRates = value;
                    _propertyChanged("ShowTestErrorRates");
                }
            }
        }

        bool _ShowTestErrorRates;

        public bool ShowNnErrorRates
        {
            get => _ShowNnErrorRates;
            set {
                if (!_ShowNnErrorRates.Equals(value)) {
                    _ShowNnErrorRates = value;
                    _propertyChanged("ShowNnErrorRates");
                }
            }
        }

        bool _ShowNnErrorRates;

        public int EpochsPerClick
        {
            get => _EpochsPerClick;
            set {
                if (value < 1) {
                    throw new ArgumentException("Must train for at least 1 epoch at a  time");
                }

                if (!Equals(_EpochsPerClick, value)) {
                    _EpochsPerClick = value;
                    _propertyChanged("EpochsPerClick");
                }
            }
        }

        int _EpochsPerClick;

        public double ItersToTrainUpto
        {
            get => _ItersToTrainUpto;
            set {
                if (value < 0.0 || double.IsInfinity(value) || double.IsNaN(value)) {
                    throw new ArgumentException("must train upto a non-negative number of iterations");
                }

                if (!_ItersToTrainUpto.Equals(value)) {
                    _ItersToTrainUpto = value;
                    _propertyChanged("ItersToTrainUpto");
                }
            }
        }

        double _ItersToTrainUpto;

        public int EpochsPerAnimation
        {
            get => _EpochsPerAnimation;
            set {
                if (value < 1) {
                    throw new ArgumentException("Must train for at least 1 epoch at a  time");
                }

                if (!_EpochsPerAnimation.Equals(value)) {
                    _EpochsPerAnimation = value;
                    _propertyChanged("EpochsPerAnimation");
                }
            }
        }

        int _EpochsPerAnimation;

        public TrainingControlValues(LvqWindowValues owner)
        {
            this.owner = owner;
            EpochsPerClick = 400;
            EpochsPerAnimation = 25;
            _ShowBoundaries = true;
            _ShowPrototypes = true;
            _CurrProjStats = StatisticsViewMode.MeanAndStderr;
            owner.LvqModels.CollectionChanged += LvqModels_CollectionChanged;
        }

        CancellationTokenSource stopTraining;
        Task trainingTask;

        public bool AnimateTraining
        {
            get => _AnimateTraining;
            set {
                if (!Equals(_AnimateTraining, value)) {
                    if (value && (SelectedLvqModel == null || SelectedDataset == null)) {
                        throw new ArgumentException("Can't animate; dataset or model is not set");
                    }

                    _AnimateTraining = value;
                    _propertyChanged("AnimateTraining");
                    if (_AnimateTraining) {
                        stopTraining = new CancellationTokenSource();
                        trainingTask = trainingTask == null
                            ? Task.Factory.StartNew(() => DoAnimatedTraining(stopTraining.Token), stopTraining.Token)
                            : trainingTask.ContinueWith(t => DoAnimatedTraining(stopTraining.Token), stopTraining.Token);
                    } else {
                        stopTraining.Cancel();
                    }
                }
            }
        }

        bool _AnimateTraining;

        void LvqModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => _propertyChanged("MatchingLvqModels");

        public void PrintCurrentStats()
        {
            var selectedModel = SelectedLvqModel;
            Console.WriteLine(selectedModel.CurrentStatsString());
        }

        public void ConfirmTraining()
        {
            var epochsToTrainFor = EpochsPerClick;
            TrainSelectedModel((dataset, model) => {
                    using (new DTimer("Training " + epochsToTrainFor + " epochs")) {
                        model.TrainEpochs(epochsToTrainFor, Owner.WindowClosingToken);
                    }
                    //var newIdx = model.GetBestSubModelIdx(dataset);
                    //owner.Dispatcher.BeginInvoke(() => { SubModelIndex = newIdx; });
                }, SelectedDataset, SelectedLvqModel
            );
        }

        public void ConfirmTrainingPrintOrder() => TrainSelectedModel((dataset, model) => model.TrainAndPrintOrder(Owner.WindowClosingToken), SelectedDataset, SelectedLvqModel);
        public void ConfirmTrainingSortedOrder() => TrainSelectedModel((dataset, model) => model.SortedTrain(Owner.WindowClosingToken), SelectedDataset, SelectedLvqModel);

        void TrainSelectedModel(Action<LvqDatasetCli, LvqMultiModel> trainImpl, LvqDatasetCli selectedDataset, LvqMultiModel selectedModel)
        {
            if (selectedDataset == null) {
                Console.WriteLine("Training aborted, no dataset selected.");
            } else if (selectedModel == null) {
                Console.WriteLine("Training aborted, no model selected.");
            } else {
                //lock (selectedModel.UpdateSyncObject) //not needed for safety, just for accurate timing
                try {
                    trainImpl(selectedDataset, selectedModel);
                    if (!Owner.WindowClosingToken.IsCancellationRequested) {
                        PrintModelTimings(selectedModel);
                        PotentialUpdate(selectedDataset, selectedModel);
                    }
                } catch (OperationCanceledException) {
                    if (!Owner.WindowClosingToken.IsCancellationRequested) {
                        throw;
                    }
                }
            }
        }

        public void TrainUptoIters()
        {
            var uptoIters = ItersToTrainUpto;
            TrainSelectedModel((dataset, model) => {
                    using (new DTimer("Training up to " + uptoIters + " iters")) {
                        model.TrainUptoIters(uptoIters, Owner.WindowClosingToken);
                    }

                    var newIdx = model.GetBestSubModelIdx();
                    owner.Dispatcher.BeginInvoke(() => {
                            if (SelectedLvqModel == model) {
                                SubModelIndex = newIdx;
                            } else {
                                model.SelectedSubModel = newIdx;
                            }
                        }
                    );
                }, SelectedDataset, SelectedLvqModel
            );
        }

        public void TrainAllUptoIters()
        {
            var uptoIters = ItersToTrainUpto;
            var allModels = Owner.LvqModels.ToArray();
            Parallel.ForEach(Partitioner.Create(allModels, true), new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = owner.WindowClosingToken }, model => {
                    var dataset = model.InitSet;
                    TrainSelectedModel((_dataset, _model) => {
                            using (new DTimer("Training up to " + uptoIters + " iters")) {
                                _model.TrainUptoIters(uptoIters, Owner.WindowClosingToken);
                            }

                            var newIdx = _model.GetBestSubModelIdx();
                            owner.Dispatcher.BeginInvoke(() => {
                                    if (SelectedLvqModel == _model) {
                                        SubModelIndex = newIdx;
                                    } else {
                                        _model.SelectedSubModel = newIdx;
                                    }
                                }
                            );
                        }, dataset, model
                    );
                }
            );
        }

        public void DoAnimatedTraining(CancellationToken token)
        {
#if BENCHMARK
            int epochsTrained = 0;
#endif
            var selectedDataset = SelectedDataset;
            var selectedModel = SelectedLvqModel;
            try {
                var overallTask = new Queue<Task>();
                while (_AnimateTraining && !token.IsCancellationRequested && !Owner.WindowClosingToken.IsCancellationRequested) {
                    var epochsToTrainFor = EpochsPerAnimation;
                    if (selectedDataset == null || selectedModel == null || epochsToTrainFor < 1) {
                        owner.Dispatcher.BeginInvoke(() => { AnimateTraining = false; });
                        break;
                    }

                    overallTask.Enqueue(
                        Task.Factory.StartNew(() => {
                                selectedModel.TrainEpochs(epochsToTrainFor, Owner.WindowClosingToken);
                                PotentialUpdate(selectedDataset, selectedModel);
                            },
                            Owner.WindowClosingToken
                        )
                    );

                    try {
                        if (overallTask.Count >= 2) {
                            overallTask.Dequeue().Wait();
                        }
                    } catch (AggregateException ae) {
                        if (!ae.InnerExceptions.All(ie => ie is OperationCanceledException)) {
                            throw;
                        }

                        break;
                    }
#if BENCHMARK
                    epochsTrained += epochsToTrainFor;
                    if (epochsTrained >= 800) {
                        _AnimateTraining = false;
                        Task.WaitAll(overallTask.ToArray());
                        owner.Dispatcher.BeginInvokeBackground(() => System.Windows.Application.Current.MainWindow.Close());
                    }
#endif
                }

                try {
                    Task.WaitAll(overallTask.ToArray());
                } catch (AggregateException ae) {
                    if (!ae.InnerExceptions.All(e => e is OperationCanceledException)) {
                        throw;
                    }
                }
            } finally {
                PrintModelTimings(selectedModel);
            }
        }

        static void PrintModelTimings(LvqMultiModel model)
        {
            var trainingStats = model.TrainingStats;
            if (trainingStats.Length >= 2) {
                var lastStat = trainingStats[trainingStats.Length - 1];
                Console.WriteLine("Avg cpu seconds per iter: {0}{1}",
                    lastStat.Value[LvqTrainingStatCli.ElapsedSecondsI] / lastStat.Value[LvqTrainingStatCli.TrainingIterationI],
                    lastStat.StandardError != null
                        ? " ~ " + lastStat.StandardError[LvqTrainingStatCli.ElapsedSecondsI] / lastStat.Value[LvqTrainingStatCli.TrainingIterationI]
                        : ""
                );
            }
        }

        void PotentialUpdate(LvqDatasetCli selectedDataset, LvqMultiModel selectedModel)
        {
            if (selectedModel == SelectedLvqModel && selectedDataset == SelectedDataset && SelectedModelUpdatedInBackgroundThread != null) {
                SelectedModelUpdatedInBackgroundThread();
            }
        }

        public void ResetLearningRate()
        {
            var selectedModel = SelectedLvqModel;
            if (selectedModel != null) {
                selectedModel.ResetLearningRate();
            }
        }

        public void UnloadModel()
        {
            var selectedModel = SelectedLvqModel;
            if (selectedModel != null) {
                SelectedLvqModel = null;
                Owner.LvqModels.Remove(selectedModel);
            }
        }

        public void UnloadDataset()
        {
            var selectedDataset = SelectedDataset;
            if (selectedDataset != null) {
                SelectedDataset = null;
                Owner.Datasets.Remove(selectedDataset);
            }
        }

        public double GetLearningRate()
        {
            var selectedModel = SelectedLvqModel;
            return selectedModel != null ? selectedModel.CurrentMeanLearningRate : 0.0;
        }

        public void DoExtendDatasetWithProtoDistances()
        {
            var model = SelectedLvqModel;
            var dataset = SelectedDataset;
            /*if (model.ModelCount != dataset.Folds()) {
                Console.WriteLine("Cannot extend dataset; model must a as many folds as dataset.");
                return;
            }*/
            if (model != null && dataset != null) {
                Owner.Dispatcher.BeginInvoke(() => Owner.Datasets.Add(dataset.ConstructByModelExtension(model.SubModels.ToArray())));
            } else {
                Console.WriteLine("You must select a dataset & model to extend a dataset by model");
            }
        }
    }
}
