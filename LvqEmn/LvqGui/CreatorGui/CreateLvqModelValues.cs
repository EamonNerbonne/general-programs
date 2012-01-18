// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using EmnExtensions.Wpf;
using LvqLibCli;
using System.Collections.Generic;

namespace LvqGui {
	public class CreateLvqModelValues : HasShorthandBase, IHasSeed {
		readonly LvqWindowValues owner;
		[NotInShorthand]
		public LvqWindowValues Owner { get { return owner; } }


		[NotInShorthand]
		public LvqDatasetCli ForDataset {
			get { return _ForDataset; }
			set { if (!Equals(_ForDataset, value)) { _ForDataset = value; _propertyChanged("ForDataset"); if (value != null) Dimensionality = Math.Min(Dimensionality, value.Dimensions); } }
		}
		LvqDatasetCli _ForDataset;

		LvqModelSettingsCli settings;

		public LvqModelType ModelType {
			get { return settings.ModelType; }
			set { if (!Equals(settings.ModelType, value)) { if (value != LvqModelType.Lgm) Dimensionality = 2; settings.ModelType = value; _propertyChanged("ModelType"); } }
		}

		public int Dimensionality {
			get { return settings.Dimensionality; }
			set {
				if (value < 0 || (ForDataset != null && value > ForDataset.Dimensions)) throw new ArgumentException("Internal dimensionality must be 0 (auto) or between 1 and the dimensions of the data.");
				if (settings.ModelType != LvqModelType.Lgm && settings.ModelType != LvqModelType.Gm && value != 2 && value != 0) throw new ArgumentException("2D Projection models must have exactly 2 internal dimensions.");
				if (!Equals(settings.Dimensionality, value)) { settings.Dimensionality = value; _propertyChanged("Dimensionality"); }
			}
		}

		public int PrototypesPerClass {
			get { return settings.PrototypesPerClass; }
			set { if (!Equals(settings.PrototypesPerClass, value)) { settings.PrototypesPerClass = value; _propertyChanged("PrototypesPerClass"); } }
		}

		public int ParallelModels {
			get { return settings.ParallelModels; }
			set {
				if (value < 1 || value > 100) throw new ArgumentException("# of models must be in range [1,100]");
				if (!settings.ParallelModels.Equals(value)) { settings.ParallelModels = value; _propertyChanged("ParallelModels"); }
			}
		}

		public int FoldOffset {
			get { return settings.FoldOffset; }
			set {
				if (!settings.FoldOffset.Equals(value)) { settings.FoldOffset = value; _propertyChanged("FoldOffset"); }
			}
		}

		public bool NoNnErrorRateTracking {
			get { return settings.NoNnErrorRateTracking; }
			set { if (!settings.NoNnErrorRateTracking.Equals(value)) { settings.NoNnErrorRateTracking = value; _propertyChanged("NoNnErrorRateTracking"); } }
		}

		public bool unnormedP
		{
			get { return settings.unnormedP; }
			set { if (!settings.unnormedP.Equals(value)) { settings.unnormedP = value; _propertyChanged("unnormedP"); } }
		}

		public bool unnormedB {
			get { return settings.unnormedB; }
			set { if (!settings.unnormedB.Equals(value)) { settings.unnormedB = value; _propertyChanged("unnormedB"); } }
		}

		public bool LocallyNormalize
		{
			get { return settings.LocallyNormalize; }
			set { if (!settings.LocallyNormalize.Equals(value)) { settings.LocallyNormalize = value; _propertyChanged("LocallyNormalize"); } }
		}

		public bool Ppca {
			get { return settings.Ppca; }
			set { if (!settings.Ppca.Equals(value)) { settings.Ppca = value; _propertyChanged("Ppca"); } }
		}

		public bool RandomInitialBorders {
			get { return settings.RandomInitialBorders; }
			set { if (!settings.RandomInitialBorders.Equals(value)) { settings.RandomInitialBorders = value; _propertyChanged("RandomInitialBorders"); } }
		}

		public bool NGu {
			get { return settings.NGu; }
			set { if (!settings.NGu.Equals(value)) { settings.NGu = value; _propertyChanged("NGu"); } }
		}

		public bool NGi {
			get { return settings.NGi; }
			set { if (!Equals(settings.NGi, value)) { settings.NGi = value; _propertyChanged("NGi"); } }
		}

		public bool Popt {
			get { return settings.Popt; }
			set { if (!Equals(settings.Popt, value)) { settings.Popt = value; _propertyChanged("Popt"); } }
		}

		public bool Bcov {
			get { return settings.Bcov; }
			set { if (!Equals(settings.Bcov, value)) { settings.Bcov = value; _propertyChanged("Bcov"); } }
		}

		public bool wGMu {
			get { return settings.wGMu; }
			set { if (!settings.wGMu.Equals(value)) { settings.wGMu = value; _propertyChanged("wGMu"); } }
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

		public double MuOffset {
			get { return settings.MuOffset; }
			set { if (!settings.MuOffset.Equals(value)) { settings.MuOffset = value; _propertyChanged("MuOffset"); } }
		}
		

		public bool SlowK {
			get { return settings.SlowK; }
			set { if (!settings.SlowK.Equals(value)) { settings.SlowK = value; _propertyChanged("SlowK"); } }
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
	new Regex(@"^([^:]*\:|\s*\\lvqseed\{)?\s*?(" +
			@"(?<ModelType>\b[A-Z][A-Za-z0-9]*)
				(\[(?<Dimensionality>[^\]]+)\]|(?<NoNnErrorRateTracking_>\+?)),
				(?<PrototypesPerClass>[0-9]+),
				(rP(?<Ppca_>\+?),)?
				(rB(?<RandomInitialBorders>\+?),)?
				(nP(?<unnormedP_>\+?),)?
				(nB(?<unnormedB_>\+?),)?
				(gn(?<LocallyNormalize_>\+?),)?
				(NG(?<NGu>\+?),)?
				(NGi(?<NGi>\+?),)?
				(Pi(?<Popt>\+?),)?
				(Bi(?<Bcov>\+?),)?
				(noB(?<wGMu>\+?),)?
				(mu(?<MuOffset>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),)?
				(lrX(?<LrScaleBad>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),)?
				(?<SlowK>\!?)
				(lr0(?<LR0>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),)?
				(lrP(?<LrScaleP>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),)?
				(lrB(?<LrScaleB>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),)?
				(\[(?<ParamsSeed_>[0-9a-fA-F]+)?\,(?<InstanceSeed_>[0-9a-fA-F]+)?\])?(\^(?<ParallelModels>[0-9]+))?(_(?<FoldOffset>[0-9]+))?\,?
			"
			+ @"
				|
				(?<ModelType>\b[A-Z][A-Za-z0-9]*)(\[(?<Dimensionality>[^\]]+)\])?
				-(?<PrototypesPerClass>[0-9]+),
				(
					(?<Ppca>Ppca,)
					|(?<RandomInitialBorders>RandomInitialBorders,)
					|(?<unnormedP>unnormedP,)
					|(?<unnormedB>unnormedB,)
					|(?<LocallyNormalize>LocallyNormalize,)
					|(?<NGu>NGu,)
					|(?<NGi>NGi,)
					|(?<Popt>Popt)
					|(?<Bcov>Bcov,)
					|(?<wGMu>wGMu,)
					|(?<SlowK>SlowK,)
					|(?<NoNnErrorRateTracking>NoNnErrorRateTracking,)
					|mu(?<MuOffset>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),
					|lrX(?<LrScaleBad>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),
					|lr0(?<LR0>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),
					|lrP(?<LrScaleP>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),
					|lrB(?<LrScaleB>[0-9]*(\.[0-9]*)?([eE]-?[0-9]+)?),
				)*
				(\[(?<ParamsSeed_>[0-9a-fA-F]+)?\,(?<InstanceSeed_>[0-9a-fA-F]+)?\])?(\^(?<ParallelModels>[0-9]+))?(_(?<FoldOffset>[0-9]+))?\,?
			"
			+ @")(--.*|\}\{[^\}]*\})?\s*$"
			,
		RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);

		public override string Shorthand {
			get {
				return settings.ToShorthand()
				+ (ForDataset == null ? "" : "--" + ForDataset.DatasetLabel);
			}
			set
			{
				settings = ParseShorthand(value);
				foreach (var group in shR.GetGroupNames())
					if (!string.IsNullOrEmpty(group))
						_propertyChanged(group);
			}
		}

		public override string ShorthandErrors { get { return ShorthandHelper.VerifyShorthand(this, shR); } }

		public static LvqModelSettingsCli ParseShorthand(string shorthand) {
			var maybeParsed= ShorthandHelper.TryParseShorthand(default(LvqModelSettingsCli), shR, shorthand);
			if (maybeParsed.HasValue)
				return maybeParsed.Value;
			else throw new ArgumentException("Can't parse: " + shorthand);
		}

		public static LvqModelSettingsCli? TryParseShorthand(string shorthand) {
			return ShorthandHelper.TryParseShorthand(default(LvqModelSettingsCli), shR, shorthand).AsNullableStruct < LvqModelSettingsCli>();
		}

		public CreateLvqModelValues(LvqWindowValues owner) {
			this.owner = owner;
			settings = new LvqModelSettingsCli();
			//this.ReseedBoth();
		}

		public Task ConfirmCreation() { return CreateSingleModel(owner, ForDataset, settings); }

		static Task CreateSingleModel(LvqWindowValues owner, LvqDatasetCli dataset, LvqModelSettingsCli settingsCopy) {
			TaskCompletionSource<object> whenDone = new TaskCompletionSource<object>();
			Task.Factory
				.StartNew(() => {
					var newModel = new LvqMultiModel(dataset, settingsCopy);
					Console.WriteLine("Created: " + newModel.ModelLabel);
					owner.Dispatcher.BeginInvoke(owner.LvqModels.Add, newModel).Completed += (s, e) => whenDone.SetResult(null);
				});
			return whenDone.Task;
		}

		public bool HasOptimizedLr { get { return DatasetResults.GetBestResult(ForDataset, settings) != null; } }
		public string OptimizeButtonText { get { return HasOptimizedLr ? "Create with Optimal LR" : "Find optimal LR"; } }

		public TestLr.LrTestingStatus OptimizedLrAllStatus { get { return TestLr.HasAllLrTestingResults(ForDataset); } }
		public string OptimizeAllButtonText { get
		{
			var status = TestLr.HasAllLrTestingResults(ForDataset);
			return status == TestLr.LrTestingStatus.AllResultsComplete
			       	? "All LR optimization complete."
			       	: status == TestLr.LrTestingStatus.SomeUnfinishedResults
			       	  	? "Waiting for unfinished results..."
			       	  	: "Find all types' Optimal LR";
		} }
		public bool OptimizedLrAllIncomplete { get { return OptimizedLrAllStatus!= TestLr.LrTestingStatus.AllResultsComplete; } }

		public void OptimizeLr() {//on gui thread.
			var settingsCopy = settings;
			settingsCopy.InstanceSeed = 0;
			settingsCopy.ParamsSeed = 1;
			var testLr = new TestLr(ForDataset);
			string shortname = testLr.ShortnameFor(settingsCopy);
			var logWindow = LogControl.ShowNewLogWindow(shortname, owner.win.ActualWidth, owner.win.ActualHeight * 0.6);
			testLr.TestLrIfNecessary(logWindow.Item2.Writer, settingsCopy, Owner.WindowClosingToken).ContinueWith(t => {
				logWindow.Item1.Dispatcher.BeginInvoke(() => logWindow.Item1.Background = Brushes.White);
			});
		}

		public void OptimizeLrAll() {//on gui thread.
			LvqDatasetCli dataset = ForDataset;
			var testLr = new TestLr(dataset);
			testLr.StartAllLrTesting(Owner.WindowClosingToken).ContinueWith(_ => Console.WriteLine("completed lr optimization for " + (dataset.DatasetLabel??"<unknown>")));
		}

		static readonly string[] depProps = new[] { "HasOptimizedLr", "OptimizeButtonText","OptimizedLrAllIncomplete", "OptimizedLrAllStatus", "OptimizeAllButtonText" };
		protected override IEnumerable<string> GloballyDependantProps { get { return base.GloballyDependantProps.Concat(depProps); } }

		public void OptimizeOrCreate() {//gui thread
			var bestResult = DatasetResults.GetBestResult(ForDataset, settings);
			if (bestResult == null)
				OptimizeLr();
			else
				CreateSingleModel(owner, ForDataset, bestResult.GetOptimizedSettings());
		}

		public void OptimizeAll() {
			var dataset = ForDataset;
			var status = OptimizedLrAllStatus;
			if (status == TestLr.LrTestingStatus.SomeUnstartedResults)
				OptimizeLrAll();
			else
				Console.WriteLine("All results already started/complete");
		}
	}
}
