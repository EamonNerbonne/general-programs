// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
using System;
using LvqLibCli;

namespace LvqGui {

	public class CreateGaussianCloudsDatasetValues : HasShorthandBase, IHasSeed {
		readonly LvqWindowValues owner;

		GaussianCloudSettings settings = new GaussianCloudSettings();
		public GaussianCloudSettings Settings { get { return settings; } set { if (settings != value) { settings = value; AllPropertiesChanged(); } } }

		public int Dimensions {
			get { return settings.Dimensions; }
			set { if (value < 1) throw new ArgumentException("Need at least one dimension"); if (!Equals(settings.Dimensions, value)) { settings.Dimensions = value; _propertyChanged("Dimensions"); } }
		}

		public int NumberOfClasses {
			get { return settings.NumberOfClasses; }
			set { if (value < 2) throw new ArgumentException("Cannot meaningfully train classifier on fewer than 2 classes"); if (!Equals(settings.NumberOfClasses, value)) { settings.NumberOfClasses = value; _propertyChanged("NumberOfClasses"); } }
		}

		public int PointsPerClass {
			get { return settings.PointsPerClass; }
			set { if (value < 1) throw new ArgumentException("Each class needs at least 1 training sample"); if (!Equals(settings.PointsPerClass, value)) { settings.PointsPerClass = value; _propertyChanged("PointsPerClass"); } }
		}

		public double ClassCenterDeviation {
			get { return settings.ClassCenterDeviation; }
			set { if (value < 0.0) throw new ArgumentException("Deviation must be positive"); if (!Equals(settings.ClassCenterDeviation, value)) { settings.ClassCenterDeviation = value; _propertyChanged("ClassCenterDeviation"); } }
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

		public override string Shorthand { get { return settings.Shorthand; } set { settings.Shorthand = value; _propertyChanged("Shorthand"); } }
		public override string ShorthandErrors { get { return settings.ShorthandErrors; } }


		public CreateGaussianCloudsDatasetValues(LvqWindowValues owner) {
			this.owner = owner;
			owner.PropertyChanged += (o, e) => {
				if (e.PropertyName == "ExtendDataByCorrelation") ExtendDataByCorrelation = owner.ExtendDataByCorrelation;
				else if (e.PropertyName == "NormalizeDimensions") NormalizeDimensions = owner.NormalizeDimensions;
			};
			PropertyChanged += (o, e) => {
				if (e.PropertyName == "ExtendDataByCorrelation") owner.ExtendDataByCorrelation = ExtendDataByCorrelation;
				else if (e.PropertyName == "NormalizeDimensions") owner.NormalizeDimensions = NormalizeDimensions;
			};
			this.ReseedBoth();
		}

		LvqDatasetCli CreateDataset() { Console.WriteLine("Creating: " + Shorthand); return settings.CreateDataset(); }


		public void ConfirmCreation() {
			owner.Dispatcher.BeginInvoke(owner.Datasets.Add, CreateDataset());
		}
	}
}