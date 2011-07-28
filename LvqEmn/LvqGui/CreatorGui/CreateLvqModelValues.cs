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
				if (settings.ModelType != LvqModelType.Lgm && value != 2 && value != 0) throw new ArgumentException("2D Projection models must have exactly 2 internal dimensions.");
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

		public bool ProjOptimalInit {
			get { return settings.ProjOptimalInit; }
			set { if (!Equals(settings.ProjOptimalInit, value)) { settings.ProjOptimalInit = value; _propertyChanged("ProjOptimalInit"); } }
		}

		public bool BLocalInit {
			get { return settings.BLocalInit; }
			set { if (!Equals(settings.BLocalInit, value)) { settings.BLocalInit = value; _propertyChanged("BLocalInit"); } }
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
	new Regex(@"^([^:]*\:|\s*\\lvqseed\{)?\s*?(" +
			@"(?<ModelType>\b[A-Z][A-Za-z0-9]*)
				(\[(?<Dimensionality>[^\]]+)\]|(?<TrackProjectionQuality>\+?)),
				(?<PrototypesPerClass>[0-9]+),
				(rP(?<RandomInitialProjection>\+?),)?
				(rB(?<RandomInitialBorders>\+?),)?
				(nP(?<NormalizeProjection>\+?),)?
				(nB(?<NormalizeBoundaries>\+?),)?
				(gn(?<GloballyNormalize>\+?),)?
				(NG(?<NgUpdateProtos>\+?),)?
				(NGi(?<NgInitializeProtos>\+?),)?
				(Pi(?<ProjOptimalInit>\+?),)?
				(Bi(?<BLocalInit>\+?),)?
				(noB(?<UpdatePointsWithoutB>\+?),)?
				(pQ(?<TrackProjectionQuality>\+?),)?
				(lrX(?<LrScaleBad>[0-9]*(\.[0-9]*)?(e[0-9]+)?),)?
				(?<SlowStartLrBad>\!?)
				(lr0(?<LR0>[0-9]*(\.[0-9]*)?(e[0-9]+)?),
				lrP(?<LrScaleP>[0-9]*(\.[0-9]*)?(e[0-9]+)?),
				lrB(?<LrScaleB>[0-9]*(\.[0-9]*)?(e[0-9]+)?),)?
				\[(?<ParamsSeed_>[0-9a-fA-F]+)\,(?<InstanceSeed_>[0-9a-fA-F]+)\](\^(?<ParallelModels>[0-9]+))?\,?"
		+ "|" +//top regex is newest
			@"(?<ModelType>\b[A-Z][A-Za-z0-9]*)
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
				(pQ(?<TrackProjectionQuality>\+?),)?
				lr0(?<LR0>[0-9]*(\.[0-9]*)?(e[0-9]+)?),
				lrP(?<LrScaleP>[0-9]*(\.[0-9]*)?(e[0-9]+)?),
				lrB(?<LrScaleB>[0-9]*(\.[0-9]*)?(e[0-9]+)?),
				lrX(?<LrScaleBad>[0-9]*(\.[0-9]*)?(e[0-9]+)?),
				(?<SlowStartLrBad>\!?)
				\[(?<ParamsSeed>[0-9]+)\:(?<InstanceSeed>[0-9]+)\]\/(?<ParallelModels>[0-9]+)\,
				(pQ(?<TrackProjectionQuality>\+?),)?"
			+ "|" +
			@"(?<ModelType>\b[A-Z][A-Za-z0-9]*)
				(\[(?<Dimensionality>[^\]]+)\])?,
				(?<PrototypesPerClass>[0-9]+),
				rP(?<RandomInitialProjection>\+?),
				(rB(?<RandomInitialBorders>\+?),)?
				nP(?<NormalizeProjection>\+?),
				(nB(?<NormalizeBoundaries>\+?),)?
				(gn(?<GloballyNormalize>\+?),)?
				(NG(?<NgUpdateProtos>\+?),?)?
				(NGi(?<NgInitializeProtos>\+?),)?
				(noB(?<UpdatePointsWithoutB>\+?),)?
				\[(?<ParamsSeed>[0-9]+)\:(?<InstanceSeed>[0-9]+)\]/(?<ParallelModels>[0-9]+),
				(pQ(?<TrackProjectionQuality>\+?),)?
				lr0(?<LR0>[0-9]*(\.[0-9]*)?(e[0-9]+)?),
				lrP(?<LrScaleP>[0-9]*(\.[0-9]*)?(e[0-9]+)?),
				lrB(?<LrScaleB>[0-9]*(\.[0-9]*)?(e[0-9]+)?),
				lrX(?<LrScaleBad>[0-9]*(\.[0-9]*)?(e[0-9]+)?),?
				(?<SlowStartLrBad>\!?)"
			+ @")(--.*|\}\{[^\}]*\})?\s*$"
			,
		RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);

		public override string Shorthand {
			get {
				return settings.ToShorthand()
				+ (ForDataset == null ? "" : "--" + ForDataset.DatasetLabel);
			}
			set {
				var updated = ShorthandHelper.ParseShorthand(this, shR, value);

				if (!updated.Contains("Dimensionality")) Dimensionality = default(LvqModelSettingsCli).Dimensionality;
				if (!updated.Contains("GloballyNormalize")) GloballyNormalize = default(LvqModelSettingsCli).GloballyNormalize;
				if (!updated.Contains("InstanceSeed")) InstanceSeed = default(LvqModelSettingsCli).InstanceSeed;
				if (!updated.Contains("LR0")) LR0 = default(LvqModelSettingsCli).LR0;
				if (!updated.Contains("LrScaleB")) LrScaleB = default(LvqModelSettingsCli).LrScaleB;
				if (!updated.Contains("LrScaleBad")) LrScaleBad = default(LvqModelSettingsCli).LrScaleBad;
				if (!updated.Contains("LrScaleP")) LrScaleP = default(LvqModelSettingsCli).LrScaleP;
				if (!updated.Contains("ModelType")) ModelType = default(LvqModelSettingsCli).ModelType;
				if (!updated.Contains("NgInitializeProtos")) NgInitializeProtos = default(LvqModelSettingsCli).NgInitializeProtos;
				if (!updated.Contains("ProjOptimalInit")) ProjOptimalInit = default(LvqModelSettingsCli).ProjOptimalInit;
				if (!updated.Contains("ProjOptimalInit")) BLocalInit = default(LvqModelSettingsCli).BLocalInit;
				if (!updated.Contains("NgUpdateProtos")) NgUpdateProtos = default(LvqModelSettingsCli).NgUpdateProtos;
				if (!updated.Contains("NormalizeBoundaries")) NormalizeBoundaries = default(LvqModelSettingsCli).NormalizeBoundaries;
				if (!updated.Contains("NormalizeProjection")) NormalizeProjection = default(LvqModelSettingsCli).NormalizeProjection;
				if (!updated.Contains("ParallelModels")) ParallelModels = default(LvqModelSettingsCli).ParallelModels;
				if (!updated.Contains("ParamsSeed")) ParamsSeed = default(LvqModelSettingsCli).ParamsSeed;
				if (!updated.Contains("PrototypesPerClass")) PrototypesPerClass = default(LvqModelSettingsCli).PrototypesPerClass;
				if (!updated.Contains("RandomInitialBorders")) RandomInitialBorders = default(LvqModelSettingsCli).RandomInitialBorders;
				if (!updated.Contains("RandomInitialProjection")) RandomInitialProjection = default(LvqModelSettingsCli).RandomInitialProjection;
				if (!updated.Contains("SlowStartLrBad")) SlowStartLrBad = default(LvqModelSettingsCli).SlowStartLrBad;
				if (!updated.Contains("TrackProjectionQuality")) TrackProjectionQuality = default(LvqModelSettingsCli).TrackProjectionQuality;
				if (!updated.Contains("UpdatePointsWithoutB")) UpdatePointsWithoutB = default(LvqModelSettingsCli).UpdatePointsWithoutB;
			}
		}

		public override string ShorthandErrors { get { return ShorthandHelper.VerifyShorthand(this, shR); } }

		public static LvqModelSettingsCli SettingsFromShorthand(string shorthand) {
			object parsedSettings = new LvqModelSettingsCli();
			ShorthandHelper.ParseShorthand(parsedSettings, shR, shorthand);
			return (LvqModelSettingsCli)parsedSettings;
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

		public bool HasOptimizedLrAll { get { return DatasetResults.GetBestResults(ForDataset, settings) != null; } }
		public string OptimizeAllButtonText { get { return HasOptimizedLrAll ? "Create all types' with Optimal LR" : "Find all types' optimal LR"; } }


		public void OptimizeLr() {//on gui thread.
			var settingsCopy = settings;
			settingsCopy.InstanceSeed = 0;
			settingsCopy.ParamsSeed = 1;
			const long iterCount = 30L * 1000L * 1000L;
			var testLr = new TestLr(iterCount, ForDataset, 3);
			string shortname = testLr.ShortnameFor(settingsCopy);
			var logWindow = LogControl.ShowNewLogWindow(shortname, owner.win.ActualWidth, owner.win.ActualHeight * 0.6);
			testLr.TestLrIfNecessary(logWindow.Item2.Writer, settingsCopy, Owner.WindowClosingToken).ContinueWith(t => {
				logWindow.Item1.Dispatcher.BeginInvoke(() => logWindow.Item1.Background = Brushes.White);
			});
		}

		public void OptimizeLrAll() {//on gui thread.
			var settingsCopy = settings;
			settingsCopy.InstanceSeed = 0;
			settingsCopy.ParamsSeed = 1;
			settingsCopy.PrototypesPerClass = 0;
			settingsCopy.ModelType = LvqModelType.Gm;
			const long iterCount = 30L * 1000L * 1000L;
			var testLr = new TestLr(iterCount, ForDataset, 3);
			testLr.StartAllLrTesting(Owner.WindowClosingToken, settingsCopy).ContinueWith(_ => Console.WriteLine("completed lr optimization for " + settingsCopy.ToShorthand()));
		}

		static readonly string[] depProps = new[] { "HasOptimizedLr", "OptimizeButtonText", "HasOptimizedLrAll", "OptimizeAllButtonText" };
		protected override IEnumerable<string> GloballyDependantProps { get { return base.GloballyDependantProps.Concat(depProps); } }

		public void OptimizeOrCreate() {//gui thread
			var bestResult = DatasetResults.GetBestResult(ForDataset, settings);
			if (bestResult == null)
				OptimizeLr();
			else
				CreateSingleModel(owner, ForDataset, bestResult.GetOptimizedSettings());
		}

		public void OptimizeAllOrCreateAll() {
			var dataset = ForDataset;
			var bestResults = DatasetResults.GetBestResults(ForDataset, settings);
			if (bestResults == null)
				OptimizeLrAll();
			else {
				foreach (var bres in bestResults)
					Console.WriteLine(bres.resultsFile.Name);


				Task.Factory
					.StartNew(() => {
						var newModels = bestResults.AsParallel().Select(res => new LvqMultiModel(dataset, res.GetOptimizedSettings())).ToArray();
						Console.WriteLine("Created: " + string.Join(", ", newModels.Select(m => m.ModelLabel)));
						owner.Dispatcher.BeginInvoke(() => {
							foreach (var newModel in newModels)
								owner.LvqModels.Add(newModel);
						});
					});
			}
		}
	}
}
