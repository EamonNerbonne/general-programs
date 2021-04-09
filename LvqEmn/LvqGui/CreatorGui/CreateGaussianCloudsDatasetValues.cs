// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

using System;
using EmnExtensions.Wpf;
using LvqGui.CoreGui;
using LvqLibCli;

namespace LvqGui.CreatorGui
{
    public sealed class CreateGaussianCloudsDatasetValues : HasShorthandBase, IHasSeed
    {
        readonly LvqWindowValues owner;
        GaussianCloudDatasetSettings settings = new();

        public GaussianCloudDatasetSettings Settings
        {
            get => settings;
            set {
                if (settings != value) {
                    settings = value;
                    AllPropertiesChanged();
                }
            }
        }

        public int Dimensions
        {
            get => settings.Dimensions;
            set {
                if (value < 1) {
                    throw new ArgumentException("Need at least one dimension");
                }

                if (!Equals(settings.Dimensions, value)) {
                    settings.Dimensions = value;
                    _propertyChanged("Dimensions");
                }
            }
        }

        public int NumberOfClasses
        {
            get => settings.NumberOfClasses;
            set {
                if (value < 2) {
                    throw new ArgumentException("Cannot meaningfully train classifier on fewer than 2 classes");
                }

                if (!Equals(settings.NumberOfClasses, value)) {
                    settings.NumberOfClasses = value;
                    _propertyChanged("NumberOfClasses");
                }
            }
        }

        public int PointsPerClass
        {
            get => settings.PointsPerClass;
            set {
                if (value < 1) {
                    throw new ArgumentException("Each class needs at least 1 training sample");
                }

                if (!Equals(settings.PointsPerClass, value)) {
                    settings.PointsPerClass = value;
                    _propertyChanged("PointsPerClass");
                }
            }
        }

        public double ClassCenterDeviation
        {
            get => settings.ClassCenterDeviation;
            set {
                if (value < 0.0) {
                    throw new ArgumentException("Deviation must be positive");
                }

                if (!Equals(settings.ClassCenterDeviation, value)) {
                    settings.ClassCenterDeviation = value;
                    _propertyChanged("ClassCenterDeviation");
                }
            }
        }

        public uint ParamsSeed
        {
            get => settings.ParamsSeed;
            set {
                if (!Equals(settings.ParamsSeed, value)) {
                    settings.ParamsSeed = value;
                    _propertyChanged("ParamsSeed");
                }
            }
        }

        public uint InstanceSeed
        {
            get => settings.InstanceSeed;
            set {
                if (!settings.InstanceSeed.Equals(value)) {
                    settings.InstanceSeed = value;
                    _propertyChanged("InstanceSeed");
                }
            }
        }

        public int Folds
        {
            get => settings.Folds;
            set {
                if (value != 0 && value < 2) {
                    throw new ArgumentException("Must have no folds (no test data) or at least 2");
                }

                if (!settings.Folds.Equals(value)) {
                    settings.Folds = value;
                    _propertyChanged("Folds");
                }
            }
        }

        public bool ExtendDataByCorrelation
        {
            get => settings.ExtendDataByCorrelation;
            set {
                if (Equals(settings.ExtendDataByCorrelation, value)) {
                    return;
                }

                settings.ExtendDataByCorrelation = value;
                _propertyChanged("ExtendDataByCorrelation");
            }
        }

        public bool NormalizeDimensions
        {
            get => settings.NormalizeDimensions;
            set {
                if (Equals(settings.NormalizeDimensions, value)) {
                    return;
                }

                settings.NormalizeDimensions = value;
                _propertyChanged("NormalizeDimensions");
            }
        }

        public bool NormalizeByScaling
        {
            get => settings.NormalizeByScaling;
            set {
                if (!Equals(settings.NormalizeByScaling, value)) {
                    settings.NormalizeByScaling = value;
                    _propertyChanged("NormalizeByScaling");
                }
            }
        }

        public override string Shorthand
        {
            get => settings.Shorthand;
            set {
                settings.Shorthand = value;
                _propertyChanged("Shorthand");
            }
        }

        public override string ShorthandErrors
            => settings.ShorthandErrors;

        public CreateGaussianCloudsDatasetValues(LvqWindowValues owner)
        {
            this.owner = owner;
            settings.NormalizeDimensions = owner.NormalizeDimensions;
            settings.NormalizeByScaling = owner.NormalizeByScaling;
            owner.PropertyChanged += (o, e) => {
                if (e.PropertyName == "ExtendDataByCorrelation") {
                    ExtendDataByCorrelation = owner.ExtendDataByCorrelation;
                } else if (e.PropertyName == "NormalizeDimensions") {
                    NormalizeDimensions = owner.NormalizeDimensions;
                } else if (e.PropertyName == "NormalizeByScaling") {
                    NormalizeByScaling = owner.NormalizeByScaling;
                }
            };
            PropertyChanged += (o, e) => {
                if (e.PropertyName == "ExtendDataByCorrelation") {
                    owner.ExtendDataByCorrelation = ExtendDataByCorrelation;
                } else if (e.PropertyName == "NormalizeDimensions") {
                    owner.NormalizeDimensions = NormalizeDimensions;
                } else if (e.PropertyName == "NormalizeByScaling") {
                    owner.NormalizeByScaling = NormalizeByScaling;
                }
            };
            //this.ReseedBoth();
        }

        LvqDatasetCli CreateDataset()
        {
            Console.WriteLine("Creating: " + Shorthand);
            return settings.CreateDataset();
        }

        public void ConfirmCreation()
            => owner.Dispatcher.BeginInvoke(owner.Datasets.Add, CreateDataset());
    }
}
