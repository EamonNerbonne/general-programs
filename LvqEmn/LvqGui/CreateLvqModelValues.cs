using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using LvqLibCli;

namespace LvqGui {
	public enum ModelType { G2m = 0, Gsm = 1, Gm = 2 }

	public class CreateLvqModelValues : INotifyPropertyChanged, IHasSeed {
		readonly LvqWindowValues owner;
		public event PropertyChangedEventHandler PropertyChanged;
		private void _propertyChanged(String propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }


		public int ClassCount {
			get { return _ClassCount; }
			set { if (value < 2) throw new ArgumentException("Need at least 2 classes to meaningfully classify"); if (!_ClassCount.Equals(value)) { _ClassCount = value; _propertyChanged("ClassCount"); } }
		}
		private int _ClassCount;


		public int Dimensions {
			get { return _Dimensions; }
			set {
				if (value < Dimensionality) throw new ArgumentException("Data dimensions must be no fewer than internal dimensions");
				if (!_Dimensions.Equals(value)) { _Dimensions = value; _propertyChanged("Dimensions"); }
			}
		}
		private int _Dimensions;


		public ModelType ModelType {
			get { return _ModelType; }
			set { if (!_ModelType.Equals(value)) { if (value != LvqGui.ModelType.Gm) Dimensionality = 2; _ModelType = value; _propertyChanged("ModelType"); } }
		}
		private ModelType _ModelType;

		public int Dimensionality {
			get { return _Dimensionality; }
			set {
				if (value < 1 || value > Dimensions) throw new ArgumentException("Internal dimensionality must be between 1 and the dimensions of the data.");
				if (_ModelType != LvqGui.ModelType.Gm && value != 2) throw new ArgumentException("2D Projection models must have exactly 2 internal dimensions.");
				if (!_Dimensionality.Equals(value)) { _Dimensionality = value; _propertyChanged("Dimensionality"); }
			}
		}
		private int _Dimensionality;

		public int PrototypesPerClass {
			get { return _PrototypesPerClass; }
			set { if (!_PrototypesPerClass.Equals(value)) { _PrototypesPerClass = value; _propertyChanged("PrototypesPerClass"); } }
		}
		private int _PrototypesPerClass;

		public uint Seed {
			get { return _Seed; }
			set { if (!_Seed.Equals(value)) { _Seed = value; _propertyChanged("Seed"); } }
		}
		private uint _Seed;


		public string CreateLabel() {
			return ModelType.ToString()
				+ (ModelType == LvqGui.ModelType.Gm ? "[" + Dimensionality + "]" : "")
				+ "-" + Dimensions + "D:"
				+ ClassCount
				+ "*" + PrototypesPerClass;
		}


		public CreateLvqModelValues(LvqWindowValues owner) {
			this.owner = owner;
			_ModelType = ModelType.G2m;
			_Dimensionality = 2;
			_Dimensions = 50;
			_PrototypesPerClass = 3;
			_ClassCount = 3;
			this.Reseed();
		}

		public LvqModelCli CreateModel() {
			return new LvqModelCli(
				rngParamsSeed: this.MakeParamsSeed(owner),
				rngInstSeed: this.MakeInstSeed(owner),
				dims: Dimensions,
				classCount: ClassCount,
				protosPerClass: PrototypesPerClass,
				modelType: (int)ModelType
				);
		}
	}
}
