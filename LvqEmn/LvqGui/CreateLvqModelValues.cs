// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using LvqLibCli;

namespace LvqGui {
	public class CreateLvqModelValues : INotifyPropertyChanged, IHasSeed, IHasShorthand {
		readonly LvqWindowValues owner;
		[NotInShorthand]
		public LvqWindowValues Owner { get { return owner; } }
		public event PropertyChangedEventHandler PropertyChanged;
		void raisePropertyChanged(string prop) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }

		private void _propertyChanged(String propertyName) {
			if (PropertyChanged != null) {
				raisePropertyChanged(propertyName);
				raisePropertyChanged("Shorthand");
				raisePropertyChanged("ShorthandErrors");
			}
		}


		[NotInShorthand]
		public LvqDatasetCli ForDataset {
			get { return _ForDataset; }
			set { if (!Equals(_ForDataset, value)) { _ForDataset = value; _propertyChanged("ForDataset"); if (value != null) Dimensionality = Math.Min(Dimensionality, value.Dimensions); } }
		}
		private LvqDatasetCli _ForDataset;

		public LvqModelType ModelType {
			get { return _ModelType; }
			set { if (!Equals(_ModelType, value)) { if (value != LvqModelType.GmModelType) Dimensionality = 2; _ModelType = value; _propertyChanged("ModelType"); } }
		}
		private LvqModelType _ModelType;

		public int Dimensionality {
			get { return _Dimensionality; }
			set {
				if (value < 0 || (ForDataset != null && value > ForDataset.Dimensions)) throw new ArgumentException("Internal dimensionality must be 0 (auto) or between 1 and the dimensions of the data.");
				if (_ModelType != LvqModelType.GmModelType && value != 2 && value != 0) throw new ArgumentException("2D Projection models must have exactly 2 internal dimensions.");
				if (!Equals(_Dimensionality, value)) { _Dimensionality = value; _propertyChanged("Dimensionality"); }
			}
		}
		private int _Dimensionality;

		public int PrototypesPerClass {
			get { return _PrototypesPerClass; }
			set { if (!Equals(_PrototypesPerClass, value)) { _PrototypesPerClass = value; _propertyChanged("PrototypesPerClass"); } }
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

		public bool NgUpdateProtos {
			get { return _NgUpdateProtos; }
			set { if (!_NgUpdateProtos.Equals(value)) { _NgUpdateProtos = value; _propertyChanged("NgUpdateProtos"); } }
		}
		private bool _NgUpdateProtos;

		public bool UpdatePointsWithoutB {
			get { return _UpdatePointsWithoutB; }
			set { if (!_UpdatePointsWithoutB.Equals(value)) { _UpdatePointsWithoutB = value; _propertyChanged("UpdatePointsWithoutB"); } }
		}
		private bool _UpdatePointsWithoutB;

		public double LrScaleP {
			get { return _LrScaleP; }
			set { if (!_LrScaleP.Equals(value)) { _LrScaleP = value; _propertyChanged("LrScaleP"); } }
		}
		private double _LrScaleP;

		public double LrScaleB {
			get { return _LrScaleB; }
			set { if (!_LrScaleB.Equals(value)) { _LrScaleB = value; _propertyChanged("LrScaleB"); } }
		}
		private double _LrScaleB;

		public double LR0 {
			get { return _LR0; }
			set { if (!_LR0.Equals(value)) { _LR0 = value; _propertyChanged("LR0"); } }
		}
		private double _LR0;

		public double LrScaleBad {
			get { return _LrScaleBad; }
			set { if (!_LrScaleBad.Equals(value)) { _LrScaleBad = value; _propertyChanged("LrScaleBad"); } }
		}
		private double _LrScaleBad;

		public uint Seed {
			get { return _Seed; }
			set { if (!Equals(_Seed, value)) { _Seed = value; _propertyChanged("Seed"); } }
		}
		private uint _Seed;

		public uint InstSeed {
			get { return _InstSeed; }
			set { if (!_InstSeed.Equals(value)) { _InstSeed = value; _propertyChanged("InstSeed"); } }
		}
		private uint _InstSeed;

		static readonly Regex shR =
	new Regex(@"
				^(\w|\s)*\:?\s*
				(?<ModelType>G[\w\d]*)
				(\[(?<Dimensionality>[^\]]+)\])?
				,(?<PrototypesPerClass>\d+)
				,rP(?<RandomInitialProjection>\+?)
				(,rB(?<RandomInitialBorders>\+?))?
				,nP(?<NormalizeProjection>\+?)
				(,nB(?<NormalizeBoundaries>\+?))?
				(,gn(?<GloballyNormalize>\+?))?
				(,NG(?<NgUpdateProtos>\+?))?
				(,noB(?<UpdatePointsWithoutB>\+?))?
				\[(?<Seed>\d+):(?<InstSeed>\d+)\]
				/(?<ParallelModels>\d+)
				(,pQ(?<TrackProjectionQuality>\+?))?
				,lr0(?<LR0>\d*\.?\d*(e\d+)?)
				,lrP(?<LrScaleP>\d*\.?\d*(e\d+)?)
				,lrB(?<LrScaleB>\d*\.?\d*(e\d+)?)
				,lrX(?<LrScaleBad>\d*\.?\d*(e\d+)?)
				(--.*)?\s*$",
		RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);

		bool isBoundaryModel { get { return ModelType == LvqModelType.G2mModelType || ModelType == LvqModelType.GmmModelType; } }

		public string Shorthand {
			get {
				return ModelType
				+ (ModelType == LvqModelType.GmModelType ? "[" + Dimensionality + "]" : "")
				+ "," + PrototypesPerClass
				+ ",rP" + (RandomInitialProjection ? "+" : "")
				+ (isBoundaryModel ? ",rB" + (RandomInitialBorders ? "+" : "") : "")
				+ ",nP" + (NormalizeProjection ? "+" : "")
				+ (isBoundaryModel ? ",nB" + (NormalizeBoundaries ? "+" : "") : "")
				+ (isBoundaryModel && NormalizeBoundaries || NormalizeProjection ? ",gn" + (GloballyNormalize ? "+" : "") : "")
				+ (ModelType != LvqModelType.GmModelType ? ",NG" + (NgUpdateProtos ? "+" : "") : "")
				+ (ModelType == LvqModelType.G2mModelType ? ",noB" + (UpdatePointsWithoutB ? "+" : "") : "")
				+ "[" + Seed + ":" + InstSeed + "]/" + ParallelModels
				+ (ModelType != LvqModelType.GmModelType ? ",pQ" + (TrackProjectionQuality ? "+" : "") : "")
				+ ",lr0" + LR0 
				+ ",lrP" + LrScaleP 
				+ ",lrB" + LrScaleB 
				+ ",lrX" + LrScaleBad 
				+ (ForDataset == null ? "" : "--" + ForDataset.DatasetLabel);
			}
			set { ShorthandHelper.ParseShorthand(this, shR, value); }
		}

		public string ShorthandErrors { get { return ShorthandHelper.VerifyShorthand(this, shR); } }


		public CreateLvqModelValues(LvqWindowValues owner) {
			this.owner = owner;
			var defaults = new LvqModelSettingsCli();

			ParallelModels = 10;

			ModelType = defaults.ModelType;
			//RngParamsSeed => this.Seed;
			//RngIterSeed => InstSeed;
			PrototypesPerClass = defaults.PrototypesPerClass;
			RandomInitialProjection = defaults.RandomInitialProjection;
			RandomInitialBorders = defaults.RandomInitialBorders;

			TrackProjectionQuality = defaults.TrackProjectionQuality;
			NormalizeProjection = defaults.NormalizeProjection;
			NormalizeBoundaries = defaults.NormalizeBoundaries;
			GloballyNormalize = defaults.GloballyNormalize;
			NgUpdateProtos = defaults.NgUpdateProtos;
			UpdatePointsWithoutB = defaults.UpdatePointsWithoutB;
			Dimensionality = defaults.Dimensionality;

			LrScaleP = defaults.LrScaleP;
			LrScaleB = defaults.LrScaleB;
			LR0 = defaults.LR0;
			LrScaleBad = defaults.LrScaleBad;
			this.ReseedBoth();
		}

		public LvqModels CreateModel() {
			Console.WriteLine("Created: " + Shorthand);

			return new LvqModels(Shorthand, ParallelModels, ForDataset, new LvqModelSettingsCli {
					ModelType = ModelType,
					RngParamsSeed = Seed,
					RngIterSeed = InstSeed,
					PrototypesPerClass = PrototypesPerClass,
					RandomInitialProjection = RandomInitialProjection,
					RandomInitialBorders = RandomInitialBorders,

					TrackProjectionQuality = TrackProjectionQuality,
					NormalizeProjection = NormalizeProjection,
					NormalizeBoundaries = NormalizeBoundaries,
					GloballyNormalize = GloballyNormalize,
					NgUpdateProtos = NgUpdateProtos,
					UpdatePointsWithoutB = UpdatePointsWithoutB,
					Dimensionality = Dimensionality,

					LrScaleP = LrScaleP,
					LrScaleB = LrScaleB,
					LR0 = LR0,
					LrScaleBad = LrScaleBad,
				});
		}

		public void ConfirmCreation() {
			owner.Dispatcher.BeginInvoke(owner.LvqModels.Add, CreateModel());
		}
	}
}
