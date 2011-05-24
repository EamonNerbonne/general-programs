﻿using System;
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
		readonly public string PatternName;

		public TestLr(uint p_offset) {
			offset = p_offset;
			followDatafolding = false;
			altLearningRates = false;
			PatternName = "base";
			datasets = Datasets(10, 1000, 1001).ToArray();
		}

		public TestLr(uint p_offset, LvqDatasetCli dataset, int folds) {
			offset = p_offset;
			followDatafolding = true;
			altLearningRates = true;
			PatternName = "custom";
			datasets = Enumerable.Repeat(dataset, folds).ToArray();
		}

		public struct ErrorRates {
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
		public ErrorRates ErrorOf(TextWriter sink, long iters, LvqModelSettingsCli settings) {
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

		static LvqModelSettingsCli CreateBasicSettings(LvqModelType type, int protos, uint rngIter, uint rngParam) {
			return new LvqModelSettingsCli {
				ModelType = type,
				Dimensionality = 2,
				PrototypesPerClass = protos,
				RandomInitialProjection = true,
				RandomInitialBorders = false,

				NormalizeProjection = true,
				NormalizeBoundaries = true,
				GloballyNormalize = true,

				NgUpdateProtos = false,
				NgInitializeProtos = false,
				UpdatePointsWithoutB = false,
	
				SlowStartLrBad = false,
				LrScaleBad = 1.0,

				ParamsSeed = rngParam,
				InstanceSeed = rngIter,
				TrackProjectionQuality = true,
			};
		}
		static LvqModelSettingsCli SetLr(LvqModelSettingsCli baseSettings, double lr0, double lrScaleP, double lrScaleB) {
			var newSettings = baseSettings.Copy();
			newSettings.LR0 = lr0;
			newSettings.LrScaleB = lrScaleB;
			newSettings.LrScaleP = lrScaleP;
			return newSettings;
		}

		public static IEnumerable<double> LogRange(double start, double end, int steps) {
			//start*exp(ln(end/start) *  i/(steps-1) )
			double lnScale = Math.Log(end / start);
			for (int i = 0; i < steps; i++)
				yield return start * Math.Exp(lnScale * ((double)i / (steps - 1)));
		}

		public void FindOptimalLr(TextWriter sink, LvqDatasetCli[] dataset, long iters, LvqModelSettingsCli settings) {
			var lr0range = altLearningRates ? LogRange(0.3 / settings.PrototypesPerClass, 0.03 / settings.PrototypesPerClass, 4) : LogRange(0.3, 0.01, 8);
			var lrPrange = altLearningRates ? LogRange(0.3 / settings.PrototypesPerClass, 0.03 / settings.PrototypesPerClass, 4) : LogRange(0.5, 0.03, 8);
			var lrBrange = settings.ModelType != LvqModelType.GgmModelType && settings.ModelType != LvqModelType.G2mModelType ? new[] { 0.0 } :
				!altLearningRates ? LogRange(0.1, 0.003, 4) :
				settings.ModelType == LvqModelType.G2mModelType ? LogRange(0.03 / settings.PrototypesPerClass, 0.003 / settings.PrototypesPerClass, 4)
				: LogRange(0.03 * settings.PrototypesPerClass, 0.003 * settings.PrototypesPerClass, 4) //!!!!
				;

			var q =
				(from lr0 in lr0range.AsParallel()
				 from lrP in lrPrange
				 from lrB in lrBrange
				 let errs = ErrorOf(sink, iters, SetLr(settings, lr0, lrP, lrB))
				 orderby errs.ErrorMean
				 select new { lr0, lrP, lrB, errs }).AsSequential();

			sink.WriteLine("lr0range:" + ObjectToCode.ComplexObjectToPseudoCode(lr0range));
			sink.WriteLine("lrPrange:" + ObjectToCode.ComplexObjectToPseudoCode(lrPrange));
			sink.WriteLine("lrBrange:" + ObjectToCode.ComplexObjectToPseudoCode(lrBrange));
			sink.WriteLine("For " + settings.ModelType + " with " + settings.PrototypesPerClass + " prototypes and " + iters + " iters training:");

			foreach (var result in q) {
				sink.Write("\n" + result.lr0.ToString("g4").PadRight(9) + "p" + result.lrP.ToString("g4").PadRight(9) + "b" + result.lrB.ToString("g4").PadRight(9) + ": "
						+ result.errs.ToString() + "[" + result.errs.cumLearningRate + "]"
					);
			}
			sink.WriteLine();
		}

		public static LvqDatasetCli PlainDataset(int folds, uint rngParam, uint rngInst, int dims, int classes, double? classsep = null) {
			return LvqDatasetCli.ConstructGaussianClouds("simplemodel", folds, false, false, null, rngParam, rngInst, dims, classes, (int)(10000 / Math.Sqrt(dims) / classes), classsep ?? 1.5);
		}
		public static LvqDatasetCli StarDataset(int folds, uint rngParam, uint rngInst, int dims, int classes, double? starsep = null, double? classrelsep = null, double? sigmanoise = null) {
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
		public static IEnumerable<LvqDatasetCli> Datasets(int folds, uint rngParam, uint rngInst) {
			yield return PlainDataset(folds, rngParam++, rngInst++, 16, 3);
			yield return PlainDataset(folds, rngParam++, rngInst++, 8, 3);
			yield return StarDataset(folds, rngParam++, rngInst++, 12, 4);
			yield return StarDataset(folds, rngParam++, rngInst++, 8, 3);
			yield return Load(folds, "segmentationNormed_combined", rngInst++);
			yield return Load(folds, "colorado", rngInst++);
			yield return Load(folds, "pendigits.train", rngInst++);
			// ReSharper restore RedundantAssignment
		}

		public void Run(TextWriter sink, long itersToRun, LvqModelSettingsCli settings) {
			sink.WriteLine("Evaluating: " + settings.ToShorthand());
			using (new DTimer(time => sink.WriteLine("Search Complete!  Took " + time)))
				FindOptimalLr(sink, datasets, itersToRun, settings);
		}

		public void RunAndSave(TextWriter sink, LvqModelSettingsCli settings, long itersToRun) {
			using (var sw = new StringWriter()) {
				var effWriter = sink == null ? (TextWriter)sw : new ForkingTextWriter(new[] { sw, sink }, false);
				Run(effWriter, itersToRun, settings);
				SaveLogFor(Shortname(settings, itersToRun), sw.ToString());
			}
		}

		public void RunAndSave(TextWriter sink, LvqModelType modeltype, int protos, long itersToRun) {
			RunAndSave(sink, CreateBasicSettings(modeltype, protos, 2 * offset, 1 + 2 * offset), itersToRun);
		}

		public static void SaveLogFor(string shortname, string logcontents) {
			var logfilepath =
				Enumerable.Range(0, 1000)
				.Select(i => shortname + (i == 0 ? "" : " (" + i + ")") + ".txt")
				.Select(filename => Path.Combine(resultsDir.FullName, filename))
				.Where(path => !File.Exists(path))
				.First();

			File.WriteAllText(logfilepath, logcontents);
		}


		public string Shortname(LvqModelSettingsCli settings, long iterCount) {
			return Shortname(settings.ModelType, settings.PrototypesPerClass, iterCount);
		}
		public string Shortname(LvqModelType modeltype, int protosPerClass, long iterCount) {
			return modeltype.ToString().Replace("ModelType", "").ToLowerInvariant() + protosPerClass + "e" + (int)(Math.Log10(iterCount) + 0.5) + PatternName + offset;
		}

		public IEnumerable<LvqModelType> ModelTypes { get { return (LvqModelType[])Enum.GetValues(typeof(LvqModelType)); } }

		public Task StartAllLrTesting(long iterCount) {
			return Task.Factory.ContinueWhenAll(
				(
					from protoCount in new[] { 5, 1 }
					from modeltype in ModelTypes
					select Task.Factory.StartNew(() => {
						string shortname = Shortname(modeltype, protoCount, iterCount);
						Console.WriteLine("Starting " + shortname);
						using (new DTimer(shortname + " training"))
							RunAndSave(null, modeltype, protoCount, iterCount);
					}, TaskCreationOptions.LongRunning)
				).ToArray(),
				subtasks => { }
			);
		}
	}
}
