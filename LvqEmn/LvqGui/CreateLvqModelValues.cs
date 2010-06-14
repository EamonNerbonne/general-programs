using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using LvqLibCli;
using System.Text.RegularExpressions;

namespace LvqGui {
	public enum ModelType { G2m = 0, Gsm = 1, Gm = 2 }

	public class CreateLvqModelValues : INotifyPropertyChanged, IHasSeed {
		readonly LvqWindowValues owner;
		public LvqWindowValues Owner { get { return owner; } }
		public event PropertyChangedEventHandler PropertyChanged;
		private void _propertyChanged(String propertyName) { if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); PropertyChanged(this, new PropertyChangedEventArgs("Shorthand")); } }

		public LvqDatasetCli ForDataset {
			get { return _ForDataset; }
			set { if (!object.Equals(_ForDataset, value)) { _ForDataset = value; _propertyChanged("ForDataset"); if (value != null) Dimensionality = Math.Min(Dimensionality, value.Dimensions); } }
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
				if (value < 1 || (ForDataset!=null&&value > ForDataset.Dimensions)) throw new ArgumentException("Internal dimensionality must be between 1 and the dimensions of the data.");
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

		public int ParallelModels {
			get { return _ParallelModels; }
			set {
				if (value < 1 || value > 100) throw new ArgumentException("# of models must be in range [1,100]");
				if (!_ParallelModels.Equals(value)) { _ParallelModels = value; _propertyChanged("ParallelModels"); }
			}
		}
		private int _ParallelModels;



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

		static Regex shR =
	new Regex(@"^\s*(?<ModelType>(G2m|Gsm|Gm))(\[(?<Dimensionality>[^\]]+)\])?,(?<PrototypesPerClass>\d+)\[(?<Seed>\d+):(?<InstSeed>\d+)\]/(?<ParallelModels>\d+)(--.*)?\s*$",
		RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		static object[] empty = new object[] { };
		public string Shorthand {
			get {
				return ModelType.ToString()
				+ (ModelType == LvqGui.ModelType.Gm ? "[" + Dimensionality + "]" : "")
				+ "," + PrototypesPerClass + "[" + Seed + ":" + InstSeed + "]/" + ParallelModels
				+(ForDataset ==null?"": "--" + ForDataset.DatasetLabel);
			}
			set {
				if (!shR.IsMatch(value)) throw new ArgumentException("can't parse shorthand - enter manually?");
				var groups = shR.Match(value).Groups.Cast<Group>().ToArray();
				for (int i = 0; i < groups.Length; i++) {
					if (!groups[i].Success) continue;
					var prop = GetType().GetProperty(shR.GroupNameFromNumber(i));
					if (prop != null) {
						var val = prop.PropertyType.Equals(typeof(bool)) ? groups[i].Value == "?"
							: TypeDescriptor.GetConverter(prop.PropertyType).ConvertFromString(groups[i].Value);
						prop.SetValue(this, val, empty);
					}
				}
			}
		}

		public CreateLvqModelValues(LvqWindowValues owner) {
			this.owner = owner;
			_ModelType = ModelType.G2m;
			_Dimensionality = 2;
			_PrototypesPerClass = 1;
			_ParallelModels = 10;
			this.ReseedBoth();
		}

		public LvqModelCli CreateModel() {
			Console.WriteLine("Created: " + Shorthand);

			return new LvqModelCli(Shorthand,
				rngParamsSeed: Seed,
				rngInstSeed: InstSeed,
				protosPerClass: PrototypesPerClass,
				modelType: (int)ModelType,
				parallelModels:ParallelModels,
				trainingSet: ForDataset
				);
		}

		public void ConfirmCreation() {
			owner.Dispatcher.BeginInvoke(owner.LvqModels.Add, CreateModel());
		}
	}
}
