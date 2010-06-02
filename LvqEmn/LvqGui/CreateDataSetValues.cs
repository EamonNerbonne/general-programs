using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using LvqLibCli;

namespace LvqGui {

	public class CreateDatasetValues : INotifyPropertyChanged, IHasSeed {
		readonly LvqWindowValues owner;
		public event PropertyChangedEventHandler PropertyChanged;
		private void _propertyChanged(String propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }

		public int Dimensions {
			get { return _Dimensions; }
			set { if (value < 1) throw new ArgumentException("Need at least one dimension"); if (!object.Equals(_Dimensions,value)) { _Dimensions = value; _propertyChanged("Dimensions"); } }
		}
		private int _Dimensions;

		public int NumberOfClasses {
			get { return _NumberOfClasses; }
			set { if (value < 2) throw new ArgumentException("Cannot meaningfully train classifier on fewer than 2 classes"); if (!object.Equals(_NumberOfClasses,value)) { _NumberOfClasses = value; _propertyChanged("NumberOfClasses"); } }
		}
		private int _NumberOfClasses;

		public int PointsPerClass {
			get { return _PointsPerClass; }
			set { if (value < 1) throw new ArgumentException("Each class needs at least 1 training sample"); if (!object.Equals(_PointsPerClass,value)) { _PointsPerClass = value; _propertyChanged("PointsPerClass"); } }
		}
		private int _PointsPerClass;

		public double ClassCenterDeviation {
			get { return _ClassCenterDeviation; }
			set { if (value < 0.0) throw new ArgumentException("Deviation must be positive"); if (!object.Equals(_ClassCenterDeviation,value)) { _ClassCenterDeviation = value; _propertyChanged("ClassCenterDeviation"); } }
		}
		private double _ClassCenterDeviation;

		public uint Seed {
			get { return _Seed; }
			set { if (!object.Equals(_Seed,value)) { _Seed = value; _propertyChanged("Seed"); } }
		}
		private uint _Seed;

		public uint InstSeed {
			get { return _InstSeed; }
			set { if (!_InstSeed.Equals(value)) { _InstSeed = value; _propertyChanged("InstSeed"); } }
		}
		private uint _InstSeed;

		

		public string CreateLabel() {
			return "norm-" + Dimensions + "D-" + NumberOfClasses + "*" + PointsPerClass + ":" + ClassCenterDeviation.ToString("f1") + "[" + Convert.ToString(Seed, 16) + MakeCounterLabel()+ "]";
		}

		int datasetCount = 0;
		string MakeCounterLabel() {
			datasetCount++;
			return ((char)('A' + datasetCount - 1)).ToString();
		}

		public CreateDatasetValues(LvqWindowValues owner) {
			this.owner = owner;
			_NumberOfClasses = 3;
			_ClassCenterDeviation = 1.0;
#if DEBUG
			_Dimensions = 8;
			_PointsPerClass = 100;
#else
			_Dimensions = 50;
			_PointsPerClass = 3000;
#endif

			this.ReseedBoth();
		}

		LvqDatasetCli CreateDataset() {
			return LvqDatasetCli.ConstructGaussianClouds(CreateLabel(),
				rngParamsSeed:Seed,
				rngInstSeed:InstSeed,
				dims: Dimensions,
				classCount: NumberOfClasses,
				pointsPerClass: PointsPerClass,
				meansep: ClassCenterDeviation
				);
		}

		public void ConfirmCreation() {
			owner.Dispatcher.BeginInvoke(owner.Datasets.Add, CreateDataset());
		}
	}
}
