using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmnExtensions;
using EmnExtensions.Filesystem;
using EmnExtensions.DebugTools;
using EmnExtensions.MathHelpers;
using EmnExtensions.Text;
using ExpressionToCodeLib;
using LvqLibCli;
using System.Reflection;

namespace LvqGui {
	class TestLr {
		readonly bool followDatafolding;
		readonly bool altLearningRates;
		readonly uint offset;
		readonly LvqDatasetCli[] datasets;
		readonly long _itersToRun;

		public TestLr(uint p_offset, long itersToRun) {
			_itersToRun = itersToRun;
			offset = p_offset;
			followDatafolding = false;
			altLearningRates = false;
			datasets = Datasets(10, 1000, 1001).ToArray();
		}

		public TestLr(long itersToRun, LvqDatasetCli dataset, int folds) {
			_itersToRun = itersToRun;
			offset = 0;
			followDatafolding = true;
			altLearningRates = true;
			datasets = Enumerable.Repeat(dataset, folds).ToArray();
		}

		struct ErrorRates {
			public readonly double training, trainingStderr, test, testStderr, nn, nnStderr, cumLearningRate;
			public ErrorRates(LvqMultiModel.Statistic stats, int nnIdx) {
				training = stats.Value[LvqTrainingStatCli.TrainingErrorI];
				test = stats.Value[LvqTrainingStatCli.TestErrorI];
				nn = nnIdx == -1 ? double.NaN : stats.Value[nnIdx];
				trainingStderr = stats.StandardError[LvqTrainingStatCli.TrainingErrorI];
				testStderr = stats.StandardError[LvqTrainingStatCli.TestErrorI];
				nnStderr = nnIdx == -1 ? double.NaN : stats.StandardError[nnIdx];
				cumLearningRate = stats.Value[LvqTrainingStatCli.CumLearningRateI];
			}
			public double ErrorMean { get { return double.IsNaN(nnStderr) && double.IsNaN(nn) ? (training * 2 + test) / 3.0 : (training * 3 + test + nn) / 5.0; } }
			public override string ToString() {
				return Statistics.GetFormatted(training, trainingStderr, 1) + "; " +
					Statistics.GetFormatted(test, testStderr, 1) + "; " +
					Statistics.GetFormatted(nn, nnStderr, 1) + "; ";
			}
		}

		ErrorRates ErrorOf(TextWriter sink, long iters, LvqModelSettingsCli settings) {
			int nnErrorIdx = -1;

			ConcurrentBag<LvqTrainingStatCli> results = new ConcurrentBag<LvqTrainingStatCli>();

			Parallel.ForEach(datasets, new ParallelOptions { TaskScheduler = LowPriorityTaskScheduler.DefaultLowPriorityScheduler, }, (dataset, _, i) => {
				int fold = followDatafolding ? (int)i : 0;
				var model = new LvqModelCli("model", dataset, fold, settings);
				nnErrorIdx = model.TrainingStatNames.AsEnumerable().IndexOf(name => name.Contains("NN Error")); // threading irrelevant; all the same & atomic.
				model.Train((int)(iters / dataset.GetTrainingSubsetSize(fold)), dataset, fold);
				var stats = model.EvaluateStats(dataset, fold);
				results.Add(stats);
			}
			);

			var meanStats = LvqMultiModel.MeanStdErrStats(results.ToArray());
			sink.Write(".");
			return new ErrorRates(meanStats, nnErrorIdx);
		}

		public LvqModelSettingsCli CreateBasicSettings(LvqModelType type, int protos, LvqModelSettingsCli settings = null) {
			return CreateBasicSettings(type, protos, 2 * offset, 1 + 2 * offset, settings);
		}

		static LvqModelSettingsCli CreateBasicSettings(LvqModelType type, int protos, uint rngIter, uint rngParam, LvqModelSettingsCli settings) {
			var retval = settings == null ? new LvqModelSettingsCli() : settings.Copy();
			retval.ModelType = type;
			retval.PrototypesPerClass = protos;
			retval.ParamsSeed = rngParam;
			retval.InstanceSeed = rngIter;
			return retval;
		}


		static LvqModelSettingsCli SetLr(LvqModelSettingsCli baseSettings, double lr0, double lrScaleP, double lrScaleB) {
			var newSettings = baseSettings.Copy();
			newSettings.LR0 = lr0;
			newSettings.LrScaleB = lrScaleB;
			newSettings.LrScaleP = lrScaleP;
			return newSettings;
		}

		static IEnumerable<double> LogRange(double start, double end, int steps) {
			//start*exp(ln(end/start) *  i/(steps-1) )
			double lnScale = Math.Log(end / start);
			for (int i = 0; i < steps; i++)
				yield return start * Math.Exp(lnScale * ((double)i / (steps - 1)));
		}

		void FindOptimalLr(TextWriter sink, LvqModelSettingsCli settings) {
			var lr0range = altLearningRates ? LogRange(0.3 / settings.PrototypesPerClass, 0.01 / settings.PrototypesPerClass, 8) : LogRange(0.3, 0.01, 8);
			var lrPrange = altLearningRates ? LogRange(0.3 / settings.PrototypesPerClass, 0.01 / settings.PrototypesPerClass, 8) : LogRange(0.5, 0.03, 8);
			var lrBrange = settings.ModelType != LvqModelType.Ggm && settings.ModelType != LvqModelType.G2m ? new[] { 0.0 }
				: !altLearningRates ? LogRange(0.1, 0.003, 4)
				: settings.ModelType == LvqModelType.G2m ? LogRange(0.03 / settings.PrototypesPerClass, 0.001 / settings.PrototypesPerClass, 8)
				: LogRange(0.1 * settings.PrototypesPerClass, 0.003 * settings.PrototypesPerClass, 8) //!!!!
				;

			var q =
				(from lr0 in lr0range.AsParallel()
				 from lrP in lrPrange
				 from lrB in lrBrange
				 let errs = ErrorOf(sink, _itersToRun, SetLr(settings, lr0, lrP, lrB))
				 orderby errs.ErrorMean
				 select new { lr0, lrP, lrB, errs }).AsSequential();

			sink.WriteLine("lr0range:" + ObjectToCode.ComplexObjectToPseudoCode(lr0range));
			sink.WriteLine("lrPrange:" + ObjectToCode.ComplexObjectToPseudoCode(lrPrange));
			sink.WriteLine("lrBrange:" + ObjectToCode.ComplexObjectToPseudoCode(lrBrange));
			sink.WriteLine("For " + settings.ModelType + " with " + settings.PrototypesPerClass + " prototypes and " + _itersToRun + " iters training:");

			foreach (var result in q) {
				sink.Write("\n" + result.lr0.ToString("g4").PadRight(9) + "p" + result.lrP.ToString("g4").PadRight(9) + "b" + result.lrB.ToString("g4").PadRight(9) + ": "
						+ result.errs + "[" + result.errs.cumLearningRate + "]"
					);
			}
			sink.WriteLine();
		}

		static LvqDatasetCli PlainDataset(int folds, uint rngParam, uint rngInst, int dims, int classes, double? classsep = null) {
			return LvqDatasetCli.ConstructGaussianClouds("simplemodel", folds, false, false, null, rngParam, rngInst, dims, classes, (int)(10000 / Math.Sqrt(dims) / classes), classsep ?? 1.5);
		}
		static LvqDatasetCli StarDataset(int folds, uint rngParam, uint rngInst, int dims, int classes, double? starsep = null, double? classrelsep = null, double? sigmanoise = null) {
			return LvqDatasetCli.ConstructStarDataset("star", folds, false, false, null, rngParam, rngInst, dims, dims / 2, 3, classes, (int)(10000 / Math.Sqrt(dims) / classes), starsep ?? 1.5, classrelsep ?? 0.5, true, sigmanoise ?? 2.5);
		}

		static DirectoryInfo FindDir(string relpath) {
			return new FileInfo(Assembly.GetEntryAssembly().Location).Directory.ParentDirs().Select(dir => Path.Combine(dir.FullName + @"\", relpath)).Where(Directory.Exists).Select(path => new DirectoryInfo(path)).FirstOrDefault();
		}

		static readonly DirectoryInfo dataDir = FindDir(@"data\datasets\");
		static readonly DirectoryInfo resultsDir = FindDir(@"uni\2009-Scriptie\Thesis\results\");

		static LvqDatasetCli Load(int folds, string name, uint rngInst) {
			var dataFile = dataDir.GetFiles(name + ".data").FirstOrDefault();
			return LoadDatasetImpl.LoadData(dataFile, false, false, rngInst, folds, null);
		}

		// ReSharper disable RedundantAssignment
		static IEnumerable<LvqDatasetCli> Datasets(int folds, uint rngParam, uint rngInst) {
			yield return PlainDataset(folds, rngParam++, rngInst++, 16, 3);
			yield return PlainDataset(folds, rngParam++, rngInst++, 8, 3);
			yield return StarDataset(folds, rngParam++, rngInst++, 12, 4);
			yield return StarDataset(folds, rngParam++, rngInst++, 8, 3);
			yield return Load(folds, "segmentationNormed_combined", rngInst++);
			yield return Load(folds, "colorado", rngInst++);
			yield return Load(folds, "pendigits.train", rngInst++);
			// ReSharper restore RedundantAssignment
		}

		void Run(TextWriter sink, LvqModelSettingsCli settings) {
			sink.WriteLine("Evaluating: " + settings.ToShorthand());
			if (altLearningRates)
				sink.WriteLine("Against: " + datasets[0].DatasetLabel);
			using (new DTimer(time => sink.WriteLine("Search Complete!  Took " + time)))
				FindOptimalLr(sink, settings);
		}

		public void RunAndSave(TextWriter sink, LvqModelSettingsCli settings) {
			if (File.Exists(GetLogfilepath(settings).First())) {
				Console.WriteLine("already done:" + GetDatasetLabel() + "\\" + Shortname(settings));
				if (sink != null) sink.WriteLine("already done!");
			} else
				using (var sw = new StringWriter()) {
					var effWriter = sink == null ? (TextWriter)sw : new ForkingTextWriter(new[] { sw, sink }, false);
					Run(effWriter, settings);
					SaveLogFor(settings, sw.ToString());
				}
		}

		string GetDatasetLabel() { return altLearningRates ? datasets[0].DatasetLabel : "base"; }

		void SaveLogFor(LvqModelSettingsCli settings, string logcontents) {
			string logfilepath = GetLogfilepath(settings).Where(path => !File.Exists(path)).First();
			Directory.CreateDirectory(Path.GetDirectoryName(logfilepath));
			File.WriteAllText(logfilepath, logcontents);
		}

		private IEnumerable<string> GetLogfilepath(LvqModelSettingsCli settings) {
			return Enumerable.Range(0, 1000)
				.Select(i => Shortname(settings) + (i == 0 ? "" : " (" + i + ")") + ".txt")
				.Select(filename => Path.Combine(resultsDir.FullName, GetDatasetLabel() + "\\" + filename));
		}

		public string Shortname(LvqModelSettingsCli settings) {
			int pow10 = (int)(Math.Log10(_itersToRun + 0.5));
			int prefix = (int)(_itersToRun / Math.Pow(10.0, pow10) + 0.5);
			return (prefix == 1 ? "" : prefix.ToString()) + "e" + pow10 + "-" + settings.ToShorthand();
		}

		static IEnumerable<LvqModelType> ModelTypes { get { return (LvqModelType[])Enum.GetValues(typeof(LvqModelType)); } }

		public Task StartAllLrTesting(LvqModelSettingsCli baseSettings = null) {
			return Task.Factory.ContinueWhenAll(
				(
					from protoCount in new[] { 5, 1 }
					from modeltype in ModelTypes
					let settings = CreateBasicSettings(modeltype, protoCount, baseSettings)
					select Task.Factory.StartNew(() => {
						string shortname = Shortname(settings);
						Console.WriteLine("Starting " + shortname);
						using (new DTimer(shortname + " training"))
							RunAndSave(null, settings);
					}, TaskCreationOptions.LongRunning)
				).ToArray(),
				subtasks => { }
			);
		}
	}
}
