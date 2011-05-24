// ReSharper disable UnusedMember.Global
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

		LvqModelSettingsCli settings;

		public LvqModelType ModelType {
			get { return settings.ModelType; }
			set { if (!Equals(settings.ModelType, value)) { if (value != LvqModelType.LgmModelType) Dimensionality = 2; settings.ModelType = value; _propertyChanged("ModelType"); } }
		}

		public int Dimensionality {
			get { return settings.Dimensionality; }
			set {
				if (value < 0 || (ForDataset != null && value > ForDataset.Dimensions)) throw new ArgumentException("Internal dimensionality must be 0 (auto) or between 1 and the dimensions of the data.");
				if (settings.ModelType != LvqModelType.LgmModelType && value != 2 && value != 0) throw new ArgumentException("2D Projection models must have exactly 2 internal dimensions.");
				if (!Equals(settings.Dimensionality, value)) { settings.Dimensionality = value; _propertyChanged("Dimensionality"); }
			}
		}

		public int PrototypesPerClass {
			get { return settings.PrototypesPerClass; }
			set { if (!Equals(settings.PrototypesPerClass, value)) { settings.PrototypesPerClass = value; _propertyChanged("PrototypesPerClass"); } }
		}

		public int ParallelModels {
			get { return _ParallelModels; }
			set {
				if (value < 1 || value > 100) throw new ArgumentException("# of models must be in range [1,100]");
				if (!_ParallelModels.Equals(value)) { _ParallelModels = value; _propertyChanged("ParallelModels"); }
			}
		}
		int _ParallelModels;

		public bool TrackProjectionQuality {
			get { return settings.TrackProjectionQuality; }
			set { if (!settings.TrackProjectionQuality.Equals(value)) { settings.TrackProjectionQuality = value; _propertyChanged("TrackProjectionQuality"); } }
		}

		public bool NormalizeProjection {
			get { return settings.NormalizeProjection; }
			set { if (!settings.NormalizeProjection.Equals(value)) { settings.NormalizeProjection = value; _propertyChanged("NormalizeProjection"); } }
		}

		public bool NormalizeBoundaries {
			get { return settings.NormalizeBoundaries; }
			set { if (!settings.NormalizeBoundaries.Equals(value)) { settings.NormalizeBoundaries = value; _propertyChanged("NormalizeBoundaries"); } }
		}

		public bool GloballyNormalize {
			get { return settings.GloballyNormalize; }
			set { if (!settings.GloballyNormalize.Equals(value)) { settings.GloballyNormalize = value; _propertyChanged("GloballyNormalize"); } }
		}

		public bool RandomInitialProjection {
			get { return settings.RandomInitialProjection; }
			set { if (!settings.RandomInitialProjection.Equals(value)) { settings.RandomInitialProjection = value; _propertyChanged("RandomInitialProjection"); } }
		}

		public bool RandomInitialBorders {
			get { return settings.RandomInitialBorders; }
			set { if (!settings.RandomInitialBorders.Equals(value)) { settings.RandomInitialBorders = value; _propertyChanged("RandomInitialBorders"); } }
		}

		public bool NgUpdateProtos {
			get { return settings.NgUpdateProtos; }
			set { if (!settings.NgUpdateProtos.Equals(value)) { settings.NgUpdateProtos = value; _propertyChanged("NgUpdateProtos"); } }
		}

		public bool NgInitializeProtos {
			get { return settings.NgInitializeProtos; }
			set { if (!Equals(settings.NgInitializeProtos, value)) { settings.NgInitializeProtos = value; _propertyChanged("NgInitializeProtos"); } }
		}

		public bool UpdatePointsWithoutB {
			get { return settings.UpdatePointsWithoutB; }
			set { if (!settings.UpdatePointsWithoutB.Equals(value)) { settings.UpdatePointsWithoutB = value; _propertyChanged("UpdatePointsWithoutB"); } }
		}

		public double LrScaleP {
			get { return settings.LrScaleP; }
			set { if (!settings.LrScaleP.Equals(value)) { settings.LrScaleP = value; _propertyChanged("LrScaleP"); } }
		}

		public double LrScaleB {
			get { return settings.LrScaleB; }
			set { if (!settings.LrScaleB.Equals(value)) { settings.LrScaleB = value; _propertyChanged("LrScaleB"); } }
		}

		public double LR0 {
			get { return settings.LR0; }
			set { if (!settings.LR0.Equals(value)) { settings.LR0 = value; _propertyChanged("LR0"); } }
		}

		public double LrScaleBad {
			get { return settings.LrScaleBad; }
			set { if (!settings.LrScaleBad.Equals(value)) { settings.LrScaleBad = value; _propertyChanged("LrScaleBad"); } }
		}

		public bool SlowStartLrBad {
			get { return settings.SlowStartLrBad; }
			set { if (!settings.SlowStartLrBad.Equals(value)) { settings.SlowStartLrBad = value; _propertyChanged("SlowStartLrBad"); } }
		}

		public uint ParamsSeed {
			get { return settings.ParamsSeed; }
			set { if (!Equals(settings.ParamsSeed, value)) { settings.ParamsSeed = value; _propertyChanged("ParamsSeed"); } }
		}

		public uint InstanceSeed {
			get { return settings.InstanceSeed; }
			set { if (!settings.InstanceSeed.Equals(value)) { settings.InstanceSeed = value; _propertyChanged("InstanceSeed"); } }
		}

		static readonly Regex shR =
	new Regex(@"
				^([^:]*\:)?\s*?
				(?<ModelType>\b[A-Z][A-Za-z0-9]*)
				(\[(?<Dimensionality>[^\]]+)\]|(?<TrackProjectionQuality>\+?)),
				(?<PrototypesPerClass>[0-9]+),
				rP(?<RandomInitialProjection>\+?),
				(rB(?<RandomInitialBorders>\+?),)?
				nP(?<NormalizeProjection>\+?),
				(nB(?<NormalizeBoundaries>\+?),)?
				(gn(?<GloballyNormalize>\+?),)?
				(NG(?<NgUpdateProtos>\+?),)?
				(NGi(?<NgInitializeProtos>\+?),)?
				(noB(?<UpdatePointsWithoutB>\+?),)?
				(pQ(?<TrackProjectionQuality>\+?),)?
				(lrX(?<LrScaleBad>[0-9]*(\.[0-9]*)?(e[0-9]+)?),)?
				(?<SlowStartLrBad>\!?)
				lr0(?<LR0>[0-9]*(\.[0-9]*)?(e[0-9]+)?),
				lrP(?<LrScaleP>[0-9]*(\.[0-9]*)?(e[0-9]+)?),
				lrB(?<LrScaleB>[0-9]*(\.[0-9]*)?(e[0-9]+)?),
				\[(?<ParamsSeed>[0-9a-fA-F]+)\,(?<InstanceSeed>[0-9a-fA-F]+)\]\^(?<ParallelModels>[0-9]+)\,
				(--.*)?\s*$",
		RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);

		public string Shorthand {
			get {
				return settings.ToShorthand() + "^" + ParallelModels + ","
				+ (ForDataset == null ? "" : "--" + ForDataset.DatasetLabel);
			}
			set {
				ShorthandHelper.ParseShorthand(this, shR, value);
				LrScaleBad = 1.0;
				ShorthandHelper.ParseShorthand(this, shR, value);
			}
		}

		public string ShorthandErrors { [MethodImpl(MethodImplOptions.NoInlining)]get { return ShorthandHelper.VerifyShorthand(this, shR); } }


		public CreateLvqModelValues(LvqWindowValues owner) {
			this.owner = owner;
			settings = new LvqModelSettingsCli();
			ParallelModels = 10;
			this.ReseedBoth();
		}

		public Task ConfirmCreation() {
			var settingsCopy = settings.Copy();
			var args = new { Shorthand, ParallelModels, ForDataset }; //for threadsafety get these now.

			TaskCompletionSource<object> whenDone = new TaskCompletionSource<object>();

			Task.Factory
				.StartNew(() => new LvqMultiModel(args.Shorthand, args.ParallelModels, args.ForDataset, settingsCopy))
				.ContinueWith(modelTask => {
					Console.WriteLine("Created: " + args.Shorthand);
					owner.Dispatcher.BeginInvoke(owner.LvqModels.Add, modelTask.Result).Completed += (s, e) => whenDone.SetResult(null);
				});

			return whenDone.Task;
		}

		internal void OptimizeLr() {//on gui thread.
			var settingsCopy = settings.Copy();
//			var args = new { Shorthand, ParallelModels, ForDataset };//for threadsafety get these now.
			long iterCount = 5000000;
			var testLr = new TestLr(0, ForDataset, ParallelModels);
			string shortname = testLr.Shortname(settingsCopy, iterCount);

			var logWindow = LogControl.ShowNewLogWindow(shortname, owner.win.ActualWidth, owner.win.ActualHeight * 0.6);

			ThreadPool.QueueUserWorkItem(_ => {
				testLr.RunAndSave(logWindow.Item2.Writer, settingsCopy, iterCount);
				logWindow.Item1.Dispatcher.BeginInvoke(() => logWindow.Item1.Background = Brushes.White);
			});
		}
	}
}
