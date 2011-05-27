// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf;
using LvqGui.CreatorGui;
using LvqLibCli;

namespace LvqGui {

	public class CreateGaussianCloudsDatasetValues : INotifyPropertyChanged, IHasSeed, IHasShorthand {
		readonly LvqWindowValues owner;
		public event PropertyChangedEventHandler PropertyChanged;
		void raisePropertyChanged(string prop) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }

		void _propertyChanged(String propertyName) {
			if (PropertyChanged != null) {
				raisePropertyChanged(propertyName);
				raisePropertyChanged("Shorthand");
				raisePropertyChanged("ShorthandErrors");
			}
		}

		GaussianCloudSettings settings = new GaussianCloudSettings();

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
			set { if (Equals(settings.ExtendDataByCorrelation, value)) return; settings.ExtendDataByCorrelation = value; owner.ExtendDataByCorrelation = value; }
		}
		public bool NormalizeDimensions {
			get { return settings.NormalizeDimensions; }
			set { if (Equals(settings.ExtendDataByCorrelation, value)) return; settings.NormalizeDimensions = value; owner.NormalizeDimensions = value; }
		}

		public string Shorthand { get { return settings.Shorthand; } set { settings.Shorthand = value; } }
		public string ShorthandErrors { get { return settings.ShorthandErrors; } }


		public CreateGaussianCloudsDatasetValues(LvqWindowValues owner) {
			this.owner = owner;
			owner.PropertyChanged += (o, e) => { if (e.PropertyName == "ExtendDataByCorrelation") { settings.ExtendDataByCorrelation = owner.ExtendDataByCorrelation; _propertyChanged("ExtendDataByCorrelation"); } };
			owner.PropertyChanged += (o, e) => { if (e.PropertyName == "NormalizeDimensions") { settings.NormalizeDimensions = owner.NormalizeDimensions; _propertyChanged("NormalizeDimensions"); } };
			this.ReseedBoth();
		}

		LvqDatasetCli CreateDataset() {
			Console.WriteLine("Created: " + Shorthand);

			// ReSharper disable RedundantArgumentName
			return LvqDatasetCli.ConstructGaussianClouds(Shorthand,
				folds: settings.Folds,
				extend: owner.ExtendDataByCorrelation,
				normalizeDims: owner.ExtendDataByCorrelation,
				colors: WpfTools.MakeDistributedColors(NumberOfClasses, new MersenneTwister((int)ParamsSeed)),
				rngParamsSeed: ParamsSeed,
				rngInstSeed: InstanceSeed,
				dims: Dimensions,
				classCount: NumberOfClasses,
				pointsPerClass: PointsPerClass,
				meansep: ClassCenterDeviation
				);
			// ReSharper restore RedundantArgumentName
		}

		public void ConfirmCreation() {
			owner.Dispatcher.BeginInvoke(owner.Datasets.Add, CreateDataset());
		}
	}
}