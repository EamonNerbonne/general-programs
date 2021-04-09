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
using LvqGui.CreatorGui;
using LvqLibCli;

namespace LvqGui.CoreGui
{
    public sealed class LvqWindowValues : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        void _propertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new(propertyName));
        }

        //public AppSettingsValues AppSettingsValues { get; private set; }
        public CreateGaussianCloudsDatasetValues CreateGaussianCloudsDatasetValues { get; }
        public CreateStarDatasetValues CreateStarDatasetValues { get; }
        public CreateLvqModelValues CreateLvqModelValues { get; }
        public TrainingControlValues TrainingControlValues { get; }
        public LoadDatasetValues LoadDatasetValues { get; }

        public bool ExtendDataByCorrelation
        {
            get => _ExtendDataByCorrelation;
            set {
                if (!_ExtendDataByCorrelation.Equals(value)) {
                    _ExtendDataByCorrelation = value;
                    _propertyChanged("ExtendDataByCorrelation");
                }
            }
        }

        bool _ExtendDataByCorrelation;

        public bool NormalizeDimensions
        {
            get => _NormalizeDimensions;
            set {
                if (!_NormalizeDimensions.Equals(value)) {
                    _NormalizeDimensions = value;
                    _propertyChanged("NormalizeDimensions");
                }
            }
        }

        bool _NormalizeDimensions = true;

        public bool NormalizeByScaling
        {
            get => _NormalizeByScaling;
            set {
                if (!Equals(_NormalizeByScaling, value)) {
                    _NormalizeByScaling = value;
                    _propertyChanged("NormalizeByScaling");
                }
            }
        }

        bool _NormalizeByScaling;
        public ObservableCollection<LvqDatasetCli> Datasets { get; }
        public ObservableCollection<LvqMultiModel> LvqModels { get; }

        public CancellationToken WindowClosingToken
            => win.ClosingToken;

        public Dispatcher Dispatcher
            => win.Dispatcher;

        public readonly LvqWindow win;

        public LvqWindowValues(LvqWindow win)
        {
            this.win = win ?? throw new ArgumentNullException(nameof(win));
            Datasets = new();
            LvqModels = new();

            //AppSettingsValues = new AppSettingsValues(this);
            CreateGaussianCloudsDatasetValues = new(this);
            CreateStarDatasetValues = new(this);
            CreateLvqModelValues = new(this);
            TrainingControlValues = new(this);
            LoadDatasetValues = new(this);

            Datasets.CollectionChanged += Datasets_CollectionChanged;
            LvqModels.CollectionChanged += LvqModels_CollectionChanged;
        }

        void LvqModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Count > 0) {
                foreach (LvqMultiModel modelGroup in e.NewItems) {
                    foreach (var subModel in modelGroup.SubModels) {
                        modelGroupLookup.Add(subModel, modelGroup);
                    }
                }

                var newModelGroup = e.NewItems.Cast<LvqMultiModel>().First();
                TrainingControlValues.SelectedDataset = newModelGroup.InitSet;
                TrainingControlValues.SelectedLvqModel = newModelGroup;
                //win.trainingTab.IsSelected = true;
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

            if (e.OldItems != null) {
                foreach (LvqMultiModel modelGroup in e.OldItems) {
                    foreach (var subModel in modelGroup.SubModels) {
                        if (!modelGroupLookup.Remove(subModel)) {
                            throw new InvalidOperationException("How can you be removing models that aren't in the lookup... ehh....?");
                        }
                    }
                }
            }
        }

        readonly Dictionary<LvqModelCli, LvqMultiModel> modelGroupLookup = new();

        public LvqMultiModel ResolveModel(LvqModelCli lastModel)
        {
            modelGroupLookup.TryGetValue(lastModel, out var multiModel);
            return multiModel;
        }

        void Datasets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null) {
                foreach (LvqDatasetCli newDataset in e.NewItems) {
                    CreateLvqModelValues.ForDataset = newDataset;
                    ThreadPool.QueueUserWorkItem(
                        o => {
                            var dataset = (LvqDatasetCli)o;
                            var errorRateAndVar = dataset.GetPcaNnErrorRate();
                            Console.WriteLine("NN error rate under PCA: {0} ~ {1}", errorRateAndVar.Item1, Math.Sqrt(errorRateAndVar.Item2));
                        },
                        newDataset
                    );
                }

                win.modelTab.IsSelected = true;
            }

            if (e.OldItems != null) {
                if (e.OldItems.Contains(CreateLvqModelValues.ForDataset)) {
                    CreateLvqModelValues.ForDataset = Datasets.LastOrDefault();
                }

                foreach (var model in LvqModels.Where(model => e.OldItems.Contains(model.InitSet)).ToArray()) {
                    LvqModels.Remove(model);
                }
            }
        }
    }
}
