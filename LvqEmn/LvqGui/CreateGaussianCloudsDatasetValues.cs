// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf;
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


		public int Dimensions {
			get { return _Dimensions; }
			set { if (value < 1) throw new ArgumentException("Need at least one dimension"); if (!Equals(_Dimensions, value)) { _Dimensions = value; _propertyChanged("Dimensions"); } }
		}
		int _Dimensions;

		public int NumberOfClasses {
			get { return _NumberOfClasses; }
			set { if (value < 2) throw new ArgumentException("Cannot meaningfully train classifier on fewer than 2 classes"); if (!Equals(_NumberOfClasses, value)) { _NumberOfClasses = value; _propertyChanged("NumberOfClasses"); } }
		}
		int _NumberOfClasses;

		public int PointsPerClass {
			get { return _PointsPerClass; }
			set { if (value < 1) throw new ArgumentException("Each class needs at least 1 training sample"); if (!Equals(_PointsPerClass, value)) { _PointsPerClass = value; _propertyChanged("PointsPerClass"); } }
		}
		int _PointsPerClass;

		public double ClassCenterDeviation {
			get { return _ClassCenterDeviation; }
			set { if (value < 0.0) throw new ArgumentException("Deviation must be positive"); if (!Equals(_ClassCenterDeviation, value)) { _ClassCenterDeviation = value; _propertyChanged("ClassCenterDeviation"); } }
		}
		double _ClassCenterDeviation;

		public uint Seed {
			get { return _Seed; }
			set { if (!Equals(_Seed, value)) { _Seed = value; _propertyChanged("Seed"); } }
		}
		uint _Seed;

		public uint InstSeed {
			get { return _InstSeed; }
			set { if (!_InstSeed.Equals(value)) { _InstSeed = value; _propertyChanged("InstSeed"); } }
		}
		uint _InstSeed;

		public int Folds {
			get { return _Folds; }
			set { if (value != 0 && value < 2) throw new ArgumentException("Must have no folds (no test data) or at least 2"); if (!_Folds.Equals(value)) { _Folds = value; _propertyChanged("Folds"); } }
		}
		int _Folds;

		public bool ExtendDataByCorrelation { get { return owner.ExtendDataByCorrelation; } set { owner.ExtendDataByCorrelation = value; } }

		static readonly Regex shR =
			new Regex(@"^\s*(.*?--)?nrm-(?<Dimensions>\d+)D(?<ExtendDataByCorrelation>\*?)-(?<NumberOfClasses>\d+)\*(?<PointsPerClass>\d+):(?<ClassCenterDeviation>[^\[]+)\[(?<Seed>\d+):(?<InstSeed>\d+)\]/(?<Folds>\d+)\s*$",
				RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);


		public string Shorthand {
			get {
				return "nrm-" + Dimensions + "D" + (ExtendDataByCorrelation ? "*" : "") + "-" + NumberOfClasses + "*" + PointsPerClass + ":" + ClassCenterDeviation.ToString("r") + "[" + Seed + ":" + InstSeed + "]/" + Folds;
			}
			set { ShorthandHelper.ParseShorthand(this, shR, value); }
		}

		public string ShorthandErrors { [MethodImpl(MethodImplOptions.NoInlining)]get { return ShorthandHelper.VerifyShorthand(this, shR); } }


		public CreateGaussianCloudsDatasetValues(LvqWindowValues owner) {
			this.owner = owner;
			owner.PropertyChanged += (o, e) => { if (e.PropertyName == "ExtendDataByCorrelation") _propertyChanged("ExtendDataByCorrelation"); };
			_Folds = 10;
			_NumberOfClasses = 3;
			_ClassCenterDeviation = 1.5;
#if DEBUG
			_Dimensions = 8;
			_PointsPerClass = 100;
#else
			_Dimensions = 24;
			_PointsPerClass = 1000;
#endif

			this.ReseedBoth();
		}

		LvqDatasetCli CreateDataset() {
			Console.WriteLine("Created: " + Shorthand);

			// ReSharper disable RedundantArgumentName
			return LvqDatasetCli.ConstructGaussianClouds(Shorthand,
				folds: _Folds,
				extend: owner.ExtendDataByCorrelation,
				colors: WpfTools.MakeDistributedColors(NumberOfClasses, new MersenneTwister((int)Seed)),
				rngParamsSeed: Seed,
				rngInstSeed: InstSeed,
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