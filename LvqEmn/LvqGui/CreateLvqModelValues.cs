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
		public LvqWindowValues Owner { get { return owner; } }
		public event PropertyChangedEventHandler PropertyChanged;
		private void _propertyChanged(String propertyName) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }

		public LvqDatasetCli ForDataset {
			get { return _ForDataset; }
			set { if (!object.Equals(_ForDataset, value)) { _ForDataset = value; _propertyChanged("ForDataset"); } }
		}
		private LvqDatasetCli _ForDataset;

		public ModelType ModelType {
			get { return _ModelType; }
			set { if (!object.Equals(_ModelType, value)) { if (value != LvqGui.ModelType.Gm) Dimensionality = 2; _ModelType = value; _propertyChanged("ModelType"); } }
		}
		private ModelType _ModelType;

		public int Dimensionality {
			get { return _Dimensionality; }
			set {
				if (value < 1 || value > ForDataset.Dimensions) throw new ArgumentException("Internal dimensionality must be between 1 and the dimensions of the data.");
				if (_ModelType != LvqGui.ModelType.Gm && value != 2) throw new ArgumentException("2D Projection models must have exactly 2 internal dimensions.");
				if (!object.Equals(_Dimensionality, value)) { _Dimensionality = value; _propertyChanged("Dimensionality"); }
			}
		}
		private int _Dimensionality;

		public int PrototypesPerClass {
			get { return _PrototypesPerClass; }
			set { if (!object.Equals(_PrototypesPerClass, value)) { _PrototypesPerClass = value; _propertyChanged("PrototypesPerClass"); } }
		}
		private int _PrototypesPerClass;

		public uint Seed {
			get { return _Seed; }
			set { if (!object.Equals(_Seed, value)) { _Seed = value; _propertyChanged("Seed"); } }
		}
		private uint _Seed;

		public uint InstSeed {
			get { return _InstSeed; }
			set { if (!_InstSeed.Equals(value)) { _InstSeed = value; _propertyChanged("InstSeed"); } }
		}
		private uint _InstSeed;



		public string CreateLabel() {
			return
				ForDataset.DatasetLabel
				+ "--" + ModelType.ToString()
				+ (ModelType == LvqGui.ModelType.Gm ? "[" + Dimensionality + "]" : "")
				+ "," + PrototypesPerClass + "[" + Convert.ToString(Seed, 16) + MakeCounterLabel() + "]";
		}
		int datasetCount = 0;
		string MakeCounterLabel() {
			datasetCount++;
			return ((char)('A' + datasetCount - 1)).ToString();
		}



		public CreateLvqModelValues(LvqWindowValues owner) {
			this.owner = owner;
			_ModelType = ModelType.G2m;
			_Dimensionality = 2;
			_PrototypesPerClass = 3;
			this.ReseedBoth();
		}

		public LvqModelCli CreateModel() {
			return new LvqModelCli(CreateLabel(),
				rngParamsSeed: Seed,
				rngInstSeed: InstSeed,
				protosPerClass: PrototypesPerClass,
				modelType: (int)ModelType,
				trainingSet: ForDataset
				);
		}

		public void ConfirmCreation() {
			owner.Dispatcher.BeginInvoke(owner.LvqModels.Add, CreateModel());
		}
	}
}
