using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using LvqLibCli;

namespace LvqGui {
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

		public LvqModelType ModelType {
			get { return _ModelType; }
			set { if (!object.Equals(_ModelType, value)) { if (value != LvqModelType.GmModelType) Dimensionality = 2; _ModelType = value; _propertyChanged("ModelType"); } }
		}
		private LvqModelType _ModelType;

		public int Dimensionality {
			get { return _Dimensionality; }
			set {
				if (value < 1 || (ForDataset != null && value > ForDataset.Dimensions)) throw new ArgumentException("Internal dimensionality must be between 1 and the dimensions of the data.");
				if (_ModelType != LvqModelType.GmModelType && value != 2) throw new ArgumentException("2D Projection models must have exactly 2 internal dimensions.");
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

		public bool TrackProjectionQuality {
			get { return _TrackProjectionQuality; }
			set { if (!_TrackProjectionQuality.Equals(value)) { _TrackProjectionQuality = value; _propertyChanged("TrackProjectionQuality"); } }
		}
		private bool _TrackProjectionQuality;

		public bool NormalizeProjection {
			get { return _NormalizeProjection; }
			set { if (!_NormalizeProjection.Equals(value)) { _NormalizeProjection = value; _propertyChanged("NormalizeProjection"); } }
		}
		private bool _NormalizeProjection;

		public bool NormalizeBoundaries {
			get { return _NormalizeBoundaries; }
			set { if (!_NormalizeBoundaries.Equals(value)) { _NormalizeBoundaries = value; _propertyChanged("NormalizeBoundaries"); } }
		}
		private bool _NormalizeBoundaries;

		public bool GloballyNormalize {
			get { return _GloballyNormalize; }
			set { if (!_GloballyNormalize.Equals(value)) { _GloballyNormalize = value; _propertyChanged("GloballyNormalize"); } }
		}
		private bool _GloballyNormalize;

		public bool RandomInitialProjection {
			get { return _RandomInitialProjection; }
			set { if (!_RandomInitialProjection.Equals(value)) { _RandomInitialProjection = value; _propertyChanged("RandomInitialProjection"); } }
		}
		private bool _RandomInitialProjection;

		public bool RandomInitialBorders {
			get { return _RandomInitialBorders; }
			set { if (!_RandomInitialBorders.Equals(value)) { _RandomInitialBorders = value; _propertyChanged("RandomInitialBorders"); } }
		}
		private bool _RandomInitialBorders;

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
	new Regex(@"^(\w|\s)*\:?\s*(?<ModelType>(G2m|Gsm|Gm))(\[(?<Dimensionality>[^\]]+)\])?,(?<PrototypesPerClass>\d+)\[(?<Seed>\d+):(?<InstSeed>\d+)\]/(?<ParallelModels>\d+)(--.*)?\s*$",
		RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		static object[] empty = new object[] { };
		public string Shorthand {
			get {
				return ModelType.ToString()
				+ (ModelType == LvqModelType.GmModelType ? "[" + Dimensionality + "]" : "")
				+ "," + PrototypesPerClass + "[" + Seed + ":" + InstSeed + "]/" + ParallelModels
				+ (ForDataset == null ? "" : "--" + ForDataset.DatasetLabel);
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
			_ModelType = LvqModelType.G2mModelType;
			_Dimensionality = 2;
			_PrototypesPerClass = 1;
			_ParallelModels = 10;
			_RandomInitialProjection = true;
			_GloballyNormalize = true;
			this.ReseedBoth();
		}

		public LvqModelCli CreateModel() {
			Console.WriteLine("Created: " + Shorthand);

			return new LvqModelCli(Shorthand,
				parallelModels: ParallelModels,
				trainingSet: ForDataset,
				modelSettings: new LvqModelSettingsCli {
					ModelType = ModelType,
					RngIterSeed = InstSeed,
					PrototypesPerClass = PrototypesPerClass,
					RngParamsSeed = this.Seed,
					TrackProjectionQuality = TrackProjectionQuality,
					NormalizeBoundaries = NormalizeBoundaries,
					GloballyNormalize = GloballyNormalize,
					NormalizeProjection = NormalizeProjection,
				});
		}

		public void ConfirmCreation() {
			owner.Dispatcher.BeginInvoke(owner.LvqModels.Add, CreateModel());
		}
	}
}
