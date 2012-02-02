// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
using System;
using System.Windows.Threading;
using LvqLibCli;

namespace LvqGui {
	public sealed class CreateStarDatasetValues : HasShorthandBase, IHasSeed {
		readonly LvqWindowValues owner;

		StarSettings settings = new StarSettings();
		public StarSettings Settings { get { return settings; } set { if (settings != value) { settings = value; AllPropertiesChanged(); } } }


		public int Dimensions {
			get { return settings.Dimensions; }
			set { if (value < settings.ClusterDimensionality) throw new ArgumentException("Data needs at least one dimension and no fewer than the clusters' dimensions"); if (!Equals(settings.Dimensions, value)) { settings.Dimensions = value; _propertyChanged("Dimensions"); } }
		}

		public int NumberOfClasses {
			get { return settings.NumberOfClasses; }
			set { if (value < 2) throw new ArgumentException("Need at least 2 classes to meaningfully train"); if (!Equals(settings.NumberOfClasses, value)) { settings.NumberOfClasses = value; _propertyChanged("NumberOfClasses"); } }
		}

		public int PointsPerClass {
			get { return settings.PointsPerClass; }
			set { if (value < 1) throw new ArgumentException("Need a positive number of points"); if (!Equals(settings.PointsPerClass, value)) { settings.PointsPerClass = value; _propertyChanged("PointsPerClass"); } }
		}

		public int NumberOfClusters {
			get { return settings.NumberOfClusters; }
			set { if (value < 1) throw new ArgumentException("Need a positive number of clusters"); if (!Equals(settings.NumberOfClusters, value)) { settings.NumberOfClusters = value; _propertyChanged("NumberOfClusters"); } }
		}

		public int ClusterDimensionality {
			get { return settings.ClusterDimensionality; }
			set { if (value < 1 || value > settings.Dimensions) throw new ArgumentException("Cluster dimensionality must be a positive number less than the absolute dimensionality"); if (!Equals(settings.ClusterDimensionality, value)) { settings.ClusterDimensionality = value; _propertyChanged("ClusterDimensionality"); } }
		}

		public bool RandomlyTransformFirst {
			get { return settings.RandomlyTransformFirst; }
			set { if (!Equals(settings.RandomlyTransformFirst, value)) { settings.RandomlyTransformFirst = value; _propertyChanged("RandomlyTransformFirst"); } }
		}

		public double ClusterCenterDeviation {
			get { return settings.ClusterCenterDeviation; }
			set { if (value < 0.0) throw new ArgumentException("Deviation must be positive"); if (!Equals(settings.ClusterCenterDeviation, value)) { settings.ClusterCenterDeviation = value; _propertyChanged("ClusterCenterDeviation"); } }
		}

		public double IntraClusterClassRelDev {
			get { return settings.IntraClusterClassRelDev; }
			set { if (value < 0.0) throw new ArgumentException("Deviation must be positive"); if (!Equals(settings.IntraClusterClassRelDev, value)) { settings.IntraClusterClassRelDev = value; _propertyChanged("IntraClusterClassRelDev"); } }
		}

		public double NoiseSigma {
			get { return settings.NoiseSigma; }
			set { if (value <= 0.0) throw new ArgumentException("Standard deviation must be positive"); if (!settings.NoiseSigma.Equals(value)) { settings.NoiseSigma = value; _propertyChanged("NoiseSigma"); } }
		}

		public double GlobalNoiseMaxSigma {
			get { return settings.GlobalNoiseMaxSigma; }
			set { if (value < 0.0) throw new ArgumentException("Standard deviation must be non-negative"); if (!settings.GlobalNoiseMaxSigma.Equals(value)) { settings.GlobalNoiseMaxSigma = value; _propertyChanged("GlobalNoiseMaxSigma"); } }
		}

		public uint ParamsSeed {
			get { return settings.ParamsSeed; }
			set { if (!Equals(settings.ParamsSeed, value)) { settings.ParamsSeed = value; _propertyChanged("ParamsSeed"); } }
		}

		public uint InstanceSeed {
			get { return settings.InstanceSeed; }
			set { if (!settings.InstanceSeed.Equals(value)) { settings.InstanceSeed = value; _propertyChanged("InstanceSeed"); } }
		}

		public int Folds {
			get { return settings.Folds; }
			set { if (value != 0 && value < 2) throw new ArgumentException("Must have no folds (no test data) or at least 2"); if (!settings.Folds.Equals(value)) { settings.Folds = value; _propertyChanged("Folds"); } }
		}

		public bool ExtendDataByCorrelation {
			get { return settings.ExtendDataByCorrelation; }
			set { if (Equals(settings.ExtendDataByCorrelation, value)) return; settings.ExtendDataByCorrelation = value; _propertyChanged("ExtendDataByCorrelation"); }
		}
		public bool NormalizeDimensions {
			get { return settings.NormalizeDimensions; }
			set { if (Equals(settings.NormalizeDimensions, value)) return; settings.NormalizeDimensions = value; _propertyChanged("NormalizeDimensions"); }
		}
		public bool NormalizeByScaling {
			get { return settings.NormalizeByScaling; }
			set { if (!object.Equals(settings.NormalizeByScaling, value)) { settings.NormalizeByScaling = value; _propertyChanged("NormalizeByScaling"); } }
		}
		


		public override string Shorthand { get { return settings.Shorthand; } set { settings.Shorthand = value; _propertyChanged("Shorthand"); } }
		public override string ShorthandErrors { get { return settings.ShorthandErrors; } }

		public CreateStarDatasetValues(LvqWindowValues owner) {
			this.owner = owner;
			settings.NormalizeDimensions = owner.NormalizeDimensions;
			settings.NormalizeByScaling = owner.NormalizeByScaling;
			owner.PropertyChanged += (o, e) => {
				if (e.PropertyName == "ExtendDataByCorrelation") ExtendDataByCorrelation = owner.ExtendDataByCorrelation;
				else if (e.PropertyName == "NormalizeDimensions") NormalizeDimensions = owner.NormalizeDimensions;
				else if (e.PropertyName == "NormalizeByScaling") NormalizeByScaling = owner.NormalizeByScaling;
			};
			PropertyChanged += (o, e) => {
				if (e.PropertyName == "ExtendDataByCorrelation") owner.ExtendDataByCorrelation = ExtendDataByCorrelation;
				else if (e.PropertyName == "NormalizeDimensions") owner.NormalizeDimensions = NormalizeDimensions;
				else if (e.PropertyName == "NormalizeByScaling") owner.NormalizeByScaling = NormalizeByScaling;
			};
			//this.ReseedBoth();
		}

		public LvqDatasetCli CreateDataset() {
			Console.WriteLine("Created: " + Shorthand);
			return settings.CreateDataset();
		}

		public DispatcherOperation ConfirmCreation() {
			return owner.Dispatcher.BeginInvoke(owner.Datasets.Add, CreateDataset());
		}
	}
}
