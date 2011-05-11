﻿// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using EmnExtensions.Wpf;
using LvqLibCli;

namespace LvqGui {
	public class CreateLvqModelValues : INotifyPropertyChanged, IHasSeed, IHasShorthand {
		readonly LvqWindowValues owner;
		[NotInShorthand]
		public LvqWindowValues Owner { get { return owner; } }
		public event PropertyChangedEventHandler PropertyChanged;
		void raisePropertyChanged(string prop) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }

		void _propertyChanged(String propertyName) {
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
		LvqDatasetCli _ForDataset;

		public LvqModelType ModelType {
			get { return _ModelType; }
			set { if (!Equals(_ModelType, value)) { if (value != LvqModelType.LgmModelType) Dimensionality = 2; _ModelType = value; _propertyChanged("ModelType"); } }
		}
		LvqModelType _ModelType;

		public int Dimensionality {
			get { return _Dimensionality; }
			set {
				if (value < 0 || (ForDataset != null && value > ForDataset.Dimensions)) throw new ArgumentException("Internal dimensionality must be 0 (auto) or between 1 and the dimensions of the data.");
				if (_ModelType != LvqModelType.LgmModelType && value != 2 && value != 0) throw new ArgumentException("2D Projection models must have exactly 2 internal dimensions.");
				if (!Equals(_Dimensionality, value)) { _Dimensionality = value; _propertyChanged("Dimensionality"); }
			}
		}
		int _Dimensionality;

		public int PrototypesPerClass {
			get { return _PrototypesPerClass; }
			set { if (!Equals(_PrototypesPerClass, value)) { _PrototypesPerClass = value; _propertyChanged("PrototypesPerClass"); } }
		}
		int _PrototypesPerClass;

		public int ParallelModels {
			get { return _ParallelModels; }
			set {
				if (value < 1 || value > 100) throw new ArgumentException("# of models must be in range [1,100]");
				if (!_ParallelModels.Equals(value)) { _ParallelModels = value; _propertyChanged("ParallelModels"); }
			}
		}
		int _ParallelModels;

		public bool TrackProjectionQuality {
			get { return _TrackProjectionQuality; }
			set { if (!_TrackProjectionQuality.Equals(value)) { _TrackProjectionQuality = value; _propertyChanged("TrackProjectionQuality"); } }
		}
		bool _TrackProjectionQuality;

		public bool NormalizeProjection {
			get { return _NormalizeProjection; }
			set { if (!_NormalizeProjection.Equals(value)) { _NormalizeProjection = value; _propertyChanged("NormalizeProjection"); } }
		}
		bool _NormalizeProjection;

		public bool NormalizeBoundaries {
			get { return _NormalizeBoundaries; }
			set { if (!_NormalizeBoundaries.Equals(value)) { _NormalizeBoundaries = value; _propertyChanged("NormalizeBoundaries"); } }
		}
		bool _NormalizeBoundaries;

		public bool GloballyNormalize {
			get { return _GloballyNormalize; }
			set { if (!_GloballyNormalize.Equals(value)) { _GloballyNormalize = value; _propertyChanged("GloballyNormalize"); } }
		}
		bool _GloballyNormalize;

		public bool RandomInitialProjection {
			get { return _RandomInitialProjection; }
			set { if (!_RandomInitialProjection.Equals(value)) { _RandomInitialProjection = value; _propertyChanged("RandomInitialProjection"); } }
		}
		bool _RandomInitialProjection;

		public bool RandomInitialBorders {
			get { return _RandomInitialBorders; }
			set { if (!_RandomInitialBorders.Equals(value)) { _RandomInitialBorders = value; _propertyChanged("RandomInitialBorders"); } }
		}
		bool _RandomInitialBorders;

		public bool NgUpdateProtos {
			get { return _NgUpdateProtos; }
			set { if (!_NgUpdateProtos.Equals(value)) { _NgUpdateProtos = value; _propertyChanged("NgUpdateProtos"); } }
		}
		bool _NgUpdateProtos;

		public bool NgInitializeProtos {
			get { return _NgInitializeProtos; }
			set { if (!Equals(_NgInitializeProtos, value)) { _NgInitializeProtos = value; _propertyChanged("NgInitializeProtos"); } }
		}
		private bool _NgInitializeProtos;



		public bool UpdatePointsWithoutB {
			get { return _UpdatePointsWithoutB; }
			set { if (!_UpdatePointsWithoutB.Equals(value)) { _UpdatePointsWithoutB = value; _propertyChanged("UpdatePointsWithoutB"); } }
		}
		bool _UpdatePointsWithoutB;

		public double LrScaleP {
			get { return _LrScaleP; }
			set { if (!_LrScaleP.Equals(value)) { _LrScaleP = value; _propertyChanged("LrScaleP"); } }
		}
		double _LrScaleP;

		public double LrScaleB {
			get { return _LrScaleB; }
			set { if (!_LrScaleB.Equals(value)) { _LrScaleB = value; _propertyChanged("LrScaleB"); } }
		}
		double _LrScaleB;

		public double LR0 {
			get { return _LR0; }
			set { if (!_LR0.Equals(value)) { _LR0 = value; _propertyChanged("LR0"); } }
		}
		double _LR0;

		public double LrScaleBad {
			get { return _LrScaleBad; }
			set { if (!_LrScaleBad.Equals(value)) { _LrScaleBad = value; _propertyChanged("LrScaleBad"); } }
		}
		double _LrScaleBad;

		public bool SlowStartLrBad {
			get { return _SlowStartLrBad; }
			set { if (!_SlowStartLrBad.Equals(value)) { _SlowStartLrBad = value; _propertyChanged("SlowStartLrBad"); } }
		}
		private bool _SlowStartLrBad;

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

		static readonly Regex shR =
	new Regex(@"
				^([^:]*\:)?\s*?
				(?<ModelType>\b[A-Z][A-Za-z0-9]*)
				(\[(?<Dimensionality>[^\]]+)\])?,
				(?<PrototypesPerClass>[0-9]+),
				rP(?<RandomInitialProjection>\+?),
				(rB(?<RandomInitialBorders>\+?),)?
				nP(?<NormalizeProjection>\+?),
				(nB(?<NormalizeBoundaries>\+?),)?
				(gn(?<GloballyNormalize>\+?),)?
				(NG(?<NgUpdateProtos>\+?),)?
				(NGi(?<NgInitializeProtos>\+?),)?
				(noB(?<UpdatePointsWithoutB>\+?),)?
				\[(?<Seed>[0-9]+)\:(?<InstSeed>[0-9]+)\]/(?<ParallelModels>[0-9]+),
				(pQ(?<TrackProjectionQuality>\+?),)?
				lr0(?<LR0>[0-9]*(\.[0-9]*)?(e[0-9]+)?),
				lrP(?<LrScaleP>[0-9]*(\.[0-9]*)?(e[0-9]+)?),
				lrB(?<LrScaleB>[0-9]*(\.[0-9]*)?(e[0-9]+)?),
				lrX(?<LrScaleBad>[0-9]*(\.[0-9]*)?(e[0-9]+)?),
				(?<SlowStartLrBad>\!?)
				(--.*)?\s*$",
		RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);

		bool isBoundaryModel { get { return ModelType == LvqModelType.G2mModelType || ModelType == LvqModelType.GgmModelType; } }

		public string Shorthand {
			get {
				return ModelType
				+ (ModelType == LvqModelType.LgmModelType ? "[" + Dimensionality + "]" : "") + ","
				+ PrototypesPerClass + ","
				+ "rP" + (RandomInitialProjection ? "+" : "") + ","
				+ (isBoundaryModel ? "rB" + (RandomInitialBorders ? "+" : "") + "," : "")
				+ "nP" + (NormalizeProjection ? "+" : "") + ","
				+ (ModelType == LvqModelType.G2mModelType ? "nB" + (NormalizeBoundaries ? "+" : "") + "," : "")
				+ (ModelType == LvqModelType.G2mModelType && NormalizeBoundaries || NormalizeProjection ? "gn" + (GloballyNormalize ? "+" : "") + "," : "")
				+ (ModelType != LvqModelType.LgmModelType ? "NG" + (NgUpdateProtos ? "+" : "") + "," : "")
				+ (PrototypesPerClass > 1 ? "NGi" + (NgInitializeProtos ? "+" : "") + "," : "")
				+ (ModelType == LvqModelType.G2mModelType ? "noB" + (UpdatePointsWithoutB ? "+" : "") + "," : "")
				+ "[" + Seed + ":" + InstSeed + "]/" + ParallelModels + ","
				+ (ModelType != LvqModelType.LgmModelType ? "pQ" + (TrackProjectionQuality ? "+" : "") + "," : "")
				+ "lr0" + LR0 + ","
				+ "lrP" + LrScaleP + ","
				+ "lrB" + LrScaleB + ","
				+ "lrX" + LrScaleBad + ","
				+ (SlowStartLrBad ? "!" : "")
				+ (ForDataset == null ? "" : "--" + ForDataset.DatasetLabel);
			}
			set { ShorthandHelper.ParseShorthand(this, shR, value); }
		}

		public string ShorthandErrors { [MethodImpl(MethodImplOptions.NoInlining)]get { return ShorthandHelper.VerifyShorthand(this, shR); } }


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
			NgInitializeProtos = defaults.NgInitializeProtos;
			UpdatePointsWithoutB = defaults.UpdatePointsWithoutB;
			Dimensionality = defaults.Dimensionality;
			SlowStartLrBad = defaults.SlowStartLrBad;

			LrScaleP = defaults.LrScaleP;
			LrScaleB = defaults.LrScaleB;
			LR0 = defaults.LR0;
			LrScaleBad = defaults.LrScaleBad;
			this.ReseedBoth();
		}

		LvqModelSettingsCli ConstructLvqModelSettings() {
			return new LvqModelSettingsCli {
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
				NgInitializeProtos = NgInitializeProtos,
				UpdatePointsWithoutB = UpdatePointsWithoutB,
				Dimensionality = Dimensionality,
				SlowStartLrBad = SlowStartLrBad,

				LrScaleP = LrScaleP,
				LrScaleB = LrScaleB,
				LR0 = LR0,
				LrScaleBad = LrScaleBad,
			};
		}

		public Task ConfirmCreation() {
			var settings = ConstructLvqModelSettings();
			var args = new { Shorthand, ParallelModels, ForDataset }; //for threadsafety get these now.

			TaskCompletionSource<object> whenDone = new TaskCompletionSource<object>();

			Task.Factory
				.StartNew(() => new LvqMultiModel(args.Shorthand, args.ParallelModels, args.ForDataset, settings))
				.ContinueWith(modelTask => {
					Console.WriteLine("Created: " + args.Shorthand);
					owner.Dispatcher.BeginInvoke(owner.LvqModels.Add, modelTask.Result).Completed += (s, e) => whenDone.SetResult(null);
				});

			return whenDone.Task;
		}

		internal void OptimizeLr() {//on gui thread.
			var settings = ConstructLvqModelSettings();
			var args = new { Shorthand, ParallelModels, ForDataset };//for threadsafety get these now.
			long iterCount = 10000000;
			var testLr = new TestLr(0, ForDataset, ParallelModels);
			string shortname = testLr.Shortname(settings, iterCount);

			var logWindow = LogControl.ShowNewLogWindow(shortname, owner.win.ActualWidth, owner.win.ActualHeight * 0.6);

			ThreadPool.QueueUserWorkItem(_ => {
				testLr.RunAndSave(logWindow.Item2.Writer, settings, iterCount);
				logWindow.Item1.Dispatcher.BeginInvoke(() => logWindow.Item1.Background = Brushes.White);
			});
		}
	}
}
