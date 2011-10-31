using System;
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
		public static LvqModelSettingsCli WithTestingChanges(this LvqModelSettingsCli settings, LvqModelType type, int protos, uint offset) {
			return settings.WithChanges(type, protos, 1 + 2 * offset, 2 * offset);
		}
		public static LvqModelSettingsCli WithTestingChanges2(this LvqModelSettingsCli settings, LvqModelType type, int protos, uint offset,
				bool rp,
				bool ngi,
				bool bi,
				bool pi,
				bool ng,
				bool slowbad
			) {
			var newsettings = settings.WithChanges(type, protos, 1 + 2 * offset, 2 * offset);
			newsettings.RandomInitialProjection = rp;
			newsettings.NgInitializeProtos = ngi;
			newsettings.NgUpdateProtos = ng;
			newsettings.BLocalInit = bi;
			newsettings.ProjOptimalInit = pi;
			newsettings.SlowStartLrBad = slowbad;
			return newsettings;
		}

		public static LvqModelSettingsCli WithLrChanges(this LvqModelSettingsCli baseSettings, double lr0, double lrScaleP, double lrScaleB) {
			var newSettings = baseSettings;
			newSettings.LR0 = lr0;
			newSettings.LrScaleB = lrScaleB;
			newSettings.LrScaleP = lrScaleP;
			return newSettings;
		}
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
	{0,0,0.96,1.23,8.06,-2.12},
	{0,0,-3.36,-2.81,-2.56,2.44},
	{0,0,0.26,-6.67,-1.85,0}},
	{
	{-11.70,-0.33,0.40,-3.52,0.58,0.10},
	{-6.84,0.82,-2.59,2.35,1.48,1.91},
	{-1.53,1.08,-2.26,-4.25,1.88,0}
	}
	};


		static double EstimateAccuracy(LvqModelType modeltype, int protoCount, bool NGi, bool NG, bool rP, bool slowbad, bool Pi, bool Bi) {
			int protoIdx = protoCount == 1 ? 0 : 1;
			int typeIdx = modeltype == LvqModelType.Ggm ? 0 : modeltype == LvqModelType.Gm ? 2 : 1;
			bool[] heurs = new[] { NGi, NG, rP, slowbad, Pi, Bi };
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

		IEnumerable<LvqModelSettingsCli> AllLrTestingSettings() {
			return
				from protoCount in new[] { 5, 1 }
				from modeltype in ModelTypes
				from rp in new[] { true, false }
				from ngi in new[] { true, false }
				//let bi=false 
				from bi in new[] { true, false }
				from pi in new[] { true, false }
				from ng in new[] { true, false }
				from slowbad in new[] { true, false }
				let relevanceCost = new[] { !rp, ngi, bi, pi, ng, slowbad }.Count(b => b)
				let estAccur = EstimateAccuracy(modeltype, protoCount, ngi, ng, rp, slowbad, pi, bi)
				orderby relevanceCost, estAccur
				select new LvqModelSettingsCli().WithTestingChanges2(modeltype, protoCount, offset, rp, ngi, bi, pi, ng, slowbad);
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
				let errs = ErrorOf(sink, _itersToRun, settings.WithLrChanges(lr0, lrP, lrB), cancel)
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
					model.Train((int)(iters / dataset.GetTrainingSubsetSize(fold)), dataset, fold, false, false);
					return model.EvaluateStats(dataset, fold);
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
						Console.WriteLine("already done:" + DatasetLabel + "\\" + ShortnameFor(settings));
						if (sink != null) sink.WriteLine("already done!");
					} else {
						Console.WriteLine("already started:" + DatasetLabel + "\\" + ShortnameFor(settings));
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
		static readonly LvqDatasetCli[] basedatasets = Datasets().ToArray();
	}
}
