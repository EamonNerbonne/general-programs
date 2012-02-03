using System;
using MoreLinq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EmnExtensions;
using EmnExtensions.Filesystem;
using EmnExtensions.MathHelpers;
using EmnExtensions.Text;
using ExpressionToCodeLib;
using LvqLibCli;

namespace LvqGui {
	public static class TestLrHelper {
		public static LvqModelSettingsCli WithTestingChanges(this LvqModelSettingsCli settings, uint offset) { return settings.WithSeeds(1 + 2 * offset, 2 * offset); }
	}

	public class TestLr {
		public readonly uint offset;
		readonly LvqDatasetCli _dataset;
		readonly long _itersToRun;
		readonly int _folds;
		readonly DirectoryInfo datasetResultsDir;

		public TestLr(long itersToRun, uint p_offset) {
			_itersToRun = itersToRun;
			offset = p_offset;
			_folds = basedatasets.Length;
			datasetResultsDir = ResultsDatasetDir(null);
		}

		public TestLr(LvqDatasetCli dataset) {
			_itersToRun = 30L * 1000L * 1000L;
			offset = 0;
			_dataset = dataset;
			_folds = 3;
			datasetResultsDir = ResultsDatasetDir(dataset);
		}


		public Task TestLrIfNecessary(TextWriter sink, LvqModelSettingsCli settings, CancellationToken cancel) {
			if (!AttemptToClaimSettings(settings, sink)) {
				var sw = new StringWriter();
				return
					Run(settings, sw, sink, cancel)
					.ContinueWith(
						t => {

							using (sw)
								if (t.Status == TaskStatus.RanToCompletion)
									SaveLogFor(settings, sw.ToString());
								else
									AttemptToUnclaimSettings(settings, sink);
						}, cancel, TaskContinuationOptions.ExecuteSynchronously, LowPriorityTaskScheduler.DefaultLowPriorityScheduler
					);
			} else {
				TaskCompletionSource<int> tc = new TaskCompletionSource<int>();
				tc.SetResult(0);
				return tc.Task;
			}
		}


		public static string ShortnameFor(long iters, LvqModelSettingsCli settings) {
			return ItersPrefix(iters) + "-" + settings.ToShorthand();
		}

		public static string ItersPrefix(long iters) {
			int pow10 = (int)(Math.Log10(iters) + Math.Log10(10.0 / 9.5));
			int prefix = (int)(iters / Math.Pow(10.0, pow10) + 0.5);
			return (prefix == 1 ? "" : prefix.ToString()) + "e" + pow10;
		}

		public string ShortnameFor(LvqModelSettingsCli settings) {
			return ShortnameFor(_itersToRun, settings);
		}
		//{-8.46,0.42,-1.11,-1.49,1.51,0.79}
		static readonly double[, ,] heurEffects = //proto,type,heur
			{
				{
					{0, 0,3.61 ,2.40 ,0, 0.48 ,2.16 ,},
					{0, 0, -8.53 ,-6.60 ,-0.27 , -5.73 ,-0.28 ,},
					{0, 0,0.30 ,-5.34 ,0, -2.48 , 0,},
				},
				{
					{-15.93 ,-0.54 ,0.97 ,-5.54 ,0, -0.48 ,-3.23 ,},
					{-9.72 ,-3.15 ,-2.71 ,4.82 ,-1.06 ,2.85 ,1.47 ,},
					{1.73 ,-0.79 ,-1.73 ,-6.40 ,0, 2.77 , 0,},
				},
			};

		static double EstimateAccuracy(LvqModelSettingsCli settings) {
			int protoIdx = settings.PrototypesPerClass == 1 ? 0 : 1;
			int typeIdx = settings.ModelType == LvqModelType.Ggm ? 0 : settings.ModelType == LvqModelType.Gm ? 2 : 1;
			bool[] heurs = new[] { settings.NGi, settings.NGu, settings.Ppca, settings.SlowK, settings.Popt, settings.Bcov };
			return heurs.Select((on, heurIdx) => on ? heurEffects[protoIdx, typeIdx, heurIdx] : 0).Sum();
		}

		public enum LrTestingStatus { SomeUnstartedResults, SomeUnfinishedResults, AllResultsComplete };

		public static LrTestingStatus HasAllLrTestingResults(LvqDatasetCli dataset) {
			var testlr = new TestLr(dataset);

			return (LrTestingStatus)
				testlr.AllLrTestingSettings().Min(settings => {
					var resultsFilePath = testlr.GetLogfilepath(settings).First();
					return (int)(!resultsFilePath.Exists
							? LrTestingStatus.SomeUnstartedResults
							: resultsFilePath.Length == 0
								? LrTestingStatus.SomeUnfinishedResults
								: LrTestingStatus.AllResultsComplete);
				});
		}

		public Task StartAllLrTesting(CancellationToken cancel) {
			var allsettings = AllLrTestingSettings();


			var testingTasks =
			(
				from settings in allsettings
				//let bi=false 
				let shortname = ShortnameFor(settings)
				select TestLrIfNecessary(null, settings, cancel)
				 ).ToArray();

			return
				Task.Factory.ContinueWhenAll(testingTasks, Task.WaitAll, cancel, TaskContinuationOptions.ExecuteSynchronously, LowPriorityTaskScheduler.DefaultLowPriorityScheduler);
		}

		static readonly LvqModelSettingsCli[] AllLrTestingSettingsNoOffset =
				(from protoCount in new[] { 5, 1 }
				 from modeltype in ModelTypes
				 from ppca in new[] { true, false }
				 from ngi in new[] { true, false }
				 from slowbad in new[] { true, false }
				 from NoB in new[] { true, false }
				 from bi in new[] { true, false }
				 from pi in new[] { true, false }
				 from ng in new[] { true, false }
				 let relevanceCost = new[] { ppca, ngi, slowbad, bi, pi, ng, NoB }.Count(b => b)
				 //where relevanceCost ==0 || relevanceCost==1 && (ngi||slowbad||!rp)
				 where relevanceCost <= 1
					|| relevanceCost <= 2 && !NoB && !ng && !pi
					|| !bi && !pi && !ng && !NoB
				 //where relevanceCost <=1
				 let settings = new LvqModelSettingsCli {
					 ModelType = modeltype,
					 PrototypesPerClass = protoCount,
					 Ppca = ppca,
					 NGi = ngi,
					 NGu = ng,
					 Bcov = bi,
					 Popt = pi,
					 SlowK = slowbad,
					 wGMu = NoB,
				 }
				 where settings == settings.Canonicalize()
				 let estAccur = EstimateAccuracy(settings)
				 orderby relevanceCost, estAccur
				 select settings).ToArray();


		IEnumerable<LvqModelSettingsCli> AllLrTestingSettings() {
			return
				from settings in AllLrTestingSettingsNoOffset
				select settings.WithTestingChanges(offset);
		}



		Task FindOptimalLr(TextWriter sink, LvqModelSettingsCli settings, CancellationToken cancel) {
			var lr0range = _dataset != null ? LogRange(0.3 / settings.PrototypesPerClass, 0.01 / settings.PrototypesPerClass, 6) : LogRange(0.3, 0.01, 8);
			var lrPrange = _dataset != null ? LogRange(0.3 / settings.PrototypesPerClass, 0.01 / settings.PrototypesPerClass, 6) : LogRange(0.5, 0.03, 8);
			var lrBrange = settings.ModelType != LvqModelType.Ggm && settings.ModelType != LvqModelType.G2m ? new[] { 0.0 }
				: _dataset == null ? LogRange(0.1, 0.003, 4)
				: settings.ModelType == LvqModelType.G2m ? LogRange(0.03 / settings.PrototypesPerClass, 0.001 / settings.PrototypesPerClass, 5)
				: LogRange(0.1 * settings.PrototypesPerClass, 0.003 * settings.PrototypesPerClass, 5) //!!!!
				;
			sink.WriteLine("lr0range:" + ObjectToCode.ComplexObjectToPseudoCode(lr0range));
			sink.WriteLine("lrPrange:" + ObjectToCode.ComplexObjectToPseudoCode(lrPrange));
			sink.WriteLine("lrBrange:" + ObjectToCode.ComplexObjectToPseudoCode(lrBrange));
			sink.WriteLine("For " + settings.ModelType + " with " + settings.PrototypesPerClass + " prototypes and " + _itersToRun + " iters training:");

			var errorRates = (
				from lr0 in lr0range
				from lrP in lrPrange
				from lrB in lrBrange
				let errs = ErrorOf(sink, _itersToRun, settings.WithLr(lr0, lrP, lrB), cancel)
				select new LrAndErrorRates { lr0 = lr0, lrP = lrP, lrB = lrB, errs = errs }
				).ToArray();

			return Task.Factory.ContinueWhenAll(errorRates.Select(run => run.errs).ToArray(),
				tasks => {
					foreach (var result in errorRates.OrderBy(err => err.errs.Result.CanonicalError))
						sink.Write("\n" + result.lr0.ToString("g4").PadRight(9) + "p" + result.lrP.ToString("g4").PadRight(9) + "b" + result.lrB.ToString("g4").PadRight(9) + ": "
								+ result.errs.Result + "[" + result.errs.Result.cumLearningRate + "]"
							);

					sink.WriteLine();
				}, cancel, TaskContinuationOptions.ExecuteSynchronously, LowPriorityTaskScheduler.DefaultLowPriorityScheduler);
		}

		static IEnumerable<double> LogRange(double start, double end, int steps) {
			//start*exp(ln(end/start) *  i/(steps-1) )
			double lnScale = Math.Log(end / start);
			for (int i = 0; i < steps; i++)
				yield return start * Math.Exp(lnScale * ((double)i / (steps - 1)));
		}

		Task<ErrorRates> ErrorOf(TextWriter sink, long iters, LvqModelSettingsCli settings, CancellationToken cancel) {
			int nnErrorIdx = -1;
			var results = new Task<LvqTrainingStatCli>[_folds];

			for (int i = 0; i < _folds; i++) {

				var dataset = _dataset ?? basedatasets[i];
				int fold = _dataset != null ? i : 0;
				results[i] = Task.Factory.StartNew(() => {
					var model = new LvqModelCli("model", dataset, fold, settings, false);

					nnErrorIdx = model.TrainingStatNames.AsEnumerable().IndexOf(name => name.Contains("NN Error")); // threading irrelevant; all the same & atomic.
					model.Train((int)(iters / dataset.PointCount(fold)), false, false);
					return model.EvaluateStats();
				}, cancel, TaskCreationOptions.PreferFairness, LowPriorityTaskScheduler.DefaultLowPriorityScheduler);
			}

			return Task.Factory.ContinueWhenAll(results, tasks => {
				sink.Write(".");
				return new ErrorRates(LvqMultiModel.MeanStdErrStats(tasks.Select(task => task.Result).ToArray()), nnErrorIdx);
			},
				cancel, TaskContinuationOptions.ExecuteSynchronously, LowPriorityTaskScheduler.DefaultLowPriorityScheduler);
		}

		class LrAndErrorRates {
			public double lr0, lrP, lrB;
			public Task<ErrorRates> errs;
		}

		public struct ErrorRates : IComparable<ErrorRates>, IComparable {
			public readonly double training, trainingStderr, test, testStderr, nn, nnStderr, cumLearningRate;
			public ErrorRates(double training, double trainingStderr, double test, double testStderr, double nn, double nnStderr, double cumLearningRate) {
				this.training = training;
				this.trainingStderr = trainingStderr;
				this.test = test;
				this.testStderr = testStderr;
				this.nn = nn;
				this.nnStderr = nnStderr;
				this.cumLearningRate = cumLearningRate;
			}
			public ErrorRates(LvqMultiModel.Statistic stats, int nnIdx) {
				training = stats.Value[LvqTrainingStatCli.TrainingErrorI];
				test = stats.Value[LvqTrainingStatCli.TestErrorI];
				nn = nnIdx == -1 ? double.NaN : stats.Value[nnIdx];
				trainingStderr = stats.StandardError[LvqTrainingStatCli.TrainingErrorI];
				testStderr = stats.StandardError[LvqTrainingStatCli.TestErrorI];
				nnStderr = nnIdx == -1 ? double.NaN : stats.StandardError[nnIdx];
				cumLearningRate = stats.Value[LvqTrainingStatCli.CumLearningRateI];
			}

			public double CanonicalError { get { return training * 0.9 + (nn.IsFinite() ? test * 0.05 + nn * 0.05 : test * 0.1); } }
			public override string ToString() {
				return Statistics.GetFormatted(training, trainingStderr, 1) + "; " +
					Statistics.GetFormatted(test, testStderr, 1) + "; " +
					Statistics.GetFormatted(nn, nnStderr, 1) + "; ";
			}

			public int CompareTo(ErrorRates other) { return CanonicalError.CompareTo(other.CanonicalError); }

			public int CompareTo(object obj) { return CompareTo((ErrorRates)obj); }
		}
		static readonly object fsSync = new object();



		bool AttemptToClaimSettings(LvqModelSettingsCli settings, TextWriter sink) {
			var saveFile = GetLogfilepath(settings).First();
			lock (fsSync)
				if (saveFile.Exists) {
					if (saveFile.Length > 0) {
						//Console.WriteLine("already done:" + DatasetLabel + "\\" + ShortnameFor(settings));
						if (sink != null) sink.WriteLine("already done!");
					} else {
						//Console.WriteLine("already started:" + DatasetLabel + "\\" + ShortnameFor(settings));
						if (sink != null) sink.WriteLine("already started!");
					}
					return true;
				} else {
					saveFile.Directory.Create();
					saveFile.Create().Close();//this is in general handy, but I'll manage it manually for now rather than leave around 0-byte files from failed runs.
					Console.WriteLine("Now starting:" + DatasetLabel + "\\" + ShortnameFor(settings) + " (" + DateTime.Now + ")");
					return false;
				}
		}

		bool AttemptToUnclaimSettings(LvqModelSettingsCli settings, TextWriter sink) {
			var saveFile = GetLogfilepath(settings).First();
			lock (fsSync)
				if (saveFile.Exists) {
					if (saveFile.Length > 0) {
						string message = "Won't cancel completed results:" + DatasetLabel + "\\" + ShortnameFor(settings);
						Console.WriteLine(message);
						if (sink != null) sink.WriteLine(message);
						return false;
					} else {
						string message = "Cancelling:" + DatasetLabel + "\\" + ShortnameFor(settings);
						Console.WriteLine(message);
						if (sink != null) sink.WriteLine(message);

						saveFile.Delete();
						return true;
					}
				} else {
					string message = "Already cancelled??? " + DatasetLabel + "\\" + ShortnameFor(settings);
					Console.WriteLine(message);
					if (sink != null) sink.WriteLine(message);
					return false;
				}
		}



		Task Run(LvqModelSettingsCli settings, StringWriter sw, TextWriter extraSink, CancellationToken cancel) {
			if (extraSink == null)
				return Run(settings, TextWriter.Synchronized(sw), cancel);
			else {
				var effWriter = new ForkingTextWriter(new[] { sw, extraSink }, false);
				return Run(settings, TextWriter.Synchronized(effWriter), cancel)
					.ContinueWith(_ => effWriter.Dispose(),
					TaskContinuationOptions.ExecuteSynchronously);
			}
		}

		Task Run(LvqModelSettingsCli settings, TextWriter sink, CancellationToken cancel) {
			sink.WriteLine("Evaluating: " + settings.ToShorthand());
			sink.WriteLine("Against: " + DatasetLabel);
			Stopwatch timer = Stopwatch.StartNew();
			LowPriorityTaskScheduler.DefaultLowPriorityScheduler.StartNewTask(() => timer = Stopwatch.StartNew());

			return FindOptimalLr(sink, settings, cancel).ContinueWith(_ => {
				double durationSec = timer.Elapsed.TotalSeconds;
				sink.WriteLine("Search Complete!  Took " + durationSec + "s");
				Console.WriteLine("Optimizing " + ShortnameFor(settings) + " took " + durationSec + "s (" + DateTime.Now + ")");
			}, TaskContinuationOptions.ExecuteSynchronously);
		}

		string DatasetLabel { get { return _dataset != null ? _dataset.DatasetLabel : "base"; } }

		void SaveLogFor(LvqModelSettingsCli settings, string logcontents) {
			FileInfo logfilepath = GetLogfilepath(settings).First(path => path.Exists || path.Length == 0);
			logfilepath.Directory.Create();
			File.WriteAllText(logfilepath.FullName, logcontents);
		}

		IEnumerable<FileInfo> GetLogfilepath(LvqModelSettingsCli settings) {
			return
				new[] { SettingsFile(settings) }.Concat(
				Enumerable.Range(1, 1000)
				.Select(i => ShortnameFor(settings) + " (" + i + ")" + ".txt")
				.Select(filename => new FileInfo(Path.Combine(resultsDir.FullName, DatasetLabel + "\\" + filename))));
		}

		FileInfo SettingsFile(LvqModelSettingsCli settings) {

			string mSettingsShorthand = settings.ToShorthand();
			string prefix = ItersPrefix(_itersToRun) + "-";

			return new FileInfo(Path.Combine(datasetResultsDir.FullName + "\\", prefix + mSettingsShorthand + ".txt"));
		}

		static DirectoryInfo ResultsDatasetDir(LvqDatasetCli dataset) {
			if (dataset == null)
				return resultsDir.CreateSubdirectory("base");
			else
				return new DirectoryInfo(resultsDir.FullName + "\\" + dataset.DatasetLabel);
		}


		public static IEnumerable<LvqModelType> ModelTypes { get { return new[] { LvqModelType.Ggm, LvqModelType.G2m, LvqModelType.Gm }; } }//  (LvqModelType[])Enum.GetValues(typeof(LvqModelType)); } }
		public static IEnumerable<int> PrototypesPerClassOpts { get { yield return 5; yield return 1; } }
		public static readonly DirectoryInfo resultsDir = FSUtil.FindDataDir(@"uni\Thesis\doc\results\", Assembly.GetAssembly(typeof(TestLr)));
		static IEnumerable<LvqDatasetCli> Datasets() {
			// ReSharper disable RedundantAssignment
			uint rngParam = 1000;
			uint rngInst = 1001;
			yield return new GaussianCloudSettings { ParamsSeed = rngParam++, InstanceSeed = rngInst++, Dimensions = 16, PointsPerClass = (int)(10000 / Math.Sqrt(16) / 3), }.CreateDataset();
			yield return new GaussianCloudSettings { ParamsSeed = rngParam++, InstanceSeed = rngInst++, Dimensions = 8, PointsPerClass = (int)(10000 / Math.Sqrt(8) / 3), }.CreateDataset();
			yield return new StarSettings { ParamsSeed = rngParam++, InstanceSeed = rngInst++, Dimensions = 12, ClusterDimensionality = 6, NumberOfClusters = 3, NumberOfClasses = 4, PointsPerClass = (int)(10000 / Math.Sqrt(12) / 4), NoiseSigma = 2.5, }.CreateDataset();
			yield return new StarSettings { ParamsSeed = rngParam++, InstanceSeed = rngInst++, Dimensions = 8, ClusterDimensionality = 4, NumberOfClusters = 3, PointsPerClass = (int)(10000 / Math.Sqrt(8) / 3), NoiseSigma = 2.5, }.CreateDataset();
			yield return LoadDatasetImpl.Load(10, "segmentationNormed_combined.data", rngInst++);
			yield return LoadDatasetImpl.Load(10, "colorado.data", rngInst++);
			yield return LoadDatasetImpl.Load(10, "pendigits.train.data", rngInst++);
			// ReSharper restore RedundantAssignment
		}
		static LvqDatasetCli[] m_basedatasets;
		static LvqDatasetCli[] basedatasets {
			get {
				return m_basedatasets ?? (m_basedatasets = Datasets().ToArray());
			}
		}

		public static LvqModelSettingsCli ChooseReasonableLr(LvqModelSettingsCli settings) {
			var options = (
				from tuple in UniformResults()
				let resSettings = tuple.Item1
				let modeltype = resSettings.ModelType
				where modeltype == settings.ModelType || settings.ModelType == LvqModelType.Lpq && resSettings.ModelType == LvqModelType.Lgm
				where (settings.PrototypesPerClass == 1) == (resSettings.PrototypesPerClass == 1)
				select resSettings
				).ToArray();
			string myshorthand = settings.WithDefaultNnTracking().WithDefaultSeeds().WithDefaultLr().ToShorthand();

			if (options.Any()) {
				var bestResults = options.MinBy(resSettings => EmnExtensions.Algorithms.Levenshtein.LevenshteinDistance(myshorthand, resSettings.WithDefaultLr().ToShorthand()));
				return settings.WithLr(bestResults.LR0, bestResults.LrScaleP, bestResults.LrScaleB);
			} else {
				return settings.ModelType == LvqModelType.Gm ? settings.WithLr(0.002, 2.0, 0.0)
					: settings.ModelType == LvqModelType.Ggm ? settings.WithLr(0.03, 0.05, 4.0)
					: settings.WithLr(0.01, 0.4, 0.006);
			}
		}

		static IEnumerable<Tuple<LvqModelSettingsCli, double>> UniformResults() {
			return
				from line in File.ReadAllLines(TestLr.resultsDir.FullName + "\\uniform-results.txt")
				let settingsOrNull = CreateLvqModelValues.TryParseShorthand(line.SubstringUntil(" "))
				where settingsOrNull.HasValue
				let settings = settingsOrNull.Value
				let geomean = double.Parse(line.SubstringAfterFirst("GeoMean: ").SubstringUntil(";"))
				group Tuple.Create(settings, geomean) by settings.WithDefaultLr().WithDefaultSeeds().WithDefaultNnTracking() into settingsGroup
				select settingsGroup.MinBy(tuple => tuple.Item2)
				;
		}
	}
}
