using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmnExtensions;
using EmnExtensions.Filesystem;
using EmnExtensions.DebugTools;
using ExpressionToCodeLib;
using LvqLibCli;
using System.Reflection;

namespace LvqGui {
	class TestLr {
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
				return TrainingControlValues.GetFormatted(training, trainingStderr) + "; " +
					TrainingControlValues.GetFormatted(test, testStderr) + "; " +
					TrainingControlValues.GetFormatted(nn, nnStderr) + "; ";
			}
		}

		public static ErrorRates ErrorOf(TextWriter sink, LvqDatasetCli[] dataset, long iters, LvqModelType type, int protos, double lr0, double lrScaleP, double lrScaleB, uint rngIter, uint rngParam) {
			int nnErrorIdx = -1;
			var severalStats = Enumerable.Range(0, dataset.Length).AsParallel().Select(fold => {
				var model = new LvqModelCli("model" + fold, dataset[fold], fold, new LvqModelSettingsCli {
					ModelType = type,
					Dimensionality = 2,
					GloballyNormalize = true,
					NormalizeBoundaries = true,
					NormalizeProjection = true,
					TrackProjectionQuality = true,

					NgInitializeProtos = false,
					NgUpdateProtos = false,
					PrototypesPerClass = protos,
					RandomInitialBorders = false,
					RandomInitialProjection = true,
					SlowStartLrBad = false,
					UpdatePointsWithoutB = false,
					LrScaleBad = 1.0,
					LR0 = lr0,
					LrScaleP = lrScaleP,
					LrScaleB = lrScaleB,
					RngIterSeed = rngIter + (uint)fold,
					RngParamsSeed = rngParam + (uint)fold,
				});
				nnErrorIdx = model.TrainingStatNames.AsEnumerable().IndexOf(name => name.Contains("NN Error")); // threading irrelevant; all the same & atomic.
				model.Train((int)(iters / dataset[fold].GetTrainingSubsetSize(fold)), dataset[fold], fold);
				var stats = model.EvaluateStats(dataset[fold], fold);
				return stats;
			}
			).ToArray();

			var meanStats = LvqMultiModel.MeanStdErrStats(severalStats);
			sink.Write(".");
			return new ErrorRates(meanStats, nnErrorIdx);
		}

		public static IEnumerable<double> LogRange(double start, double end, int steps) {
			//start*exp(ln(end/start) *  i/(steps-1) )
			double lnScale = Math.Log(end / start);
			for (int i = 0; i < steps; i++)
				yield return start * Math.Exp(lnScale * ((double)i / (steps - 1)));
		}

		public static void FindOptimalLr(TextWriter sink, LvqDatasetCli[] dataset, long iters, LvqModelType type, int protos, uint rngIter, uint rngParam) {
			var lr0range = LogRange(0.3, 0.01, 8);
			var lrPrange = LogRange(0.5, 0.03, 8);
			var lrBrange = (type == LvqModelType.GgmModelType || type == LvqModelType.G2mModelType ? LogRange(0.1, 0.003, 4) : new[] { 0.0 });

			var q =
				(from lr0 in lr0range.AsParallel()
				 from lrP in lrPrange
				 from lrB in lrBrange
				 let errs = ErrorOf(sink, dataset, iters, type, protos, lr0, lrP, lrB, rngIter, rngParam)
				 orderby errs.ErrorMean
				 select new { lr0, lrP, lrB, errs }).AsSequential();

			sink.WriteLine("lr0range:" + ObjectToCode.ComplexObjectToPseudoCode(lr0range));
			sink.WriteLine("lrPrange:" + ObjectToCode.ComplexObjectToPseudoCode(lrPrange));
			sink.WriteLine("lrBrange:" + ObjectToCode.ComplexObjectToPseudoCode(lrBrange));
			sink.WriteLine("For " + type + " with " + protos + " prototypes and " + iters + " iters training:");

			foreach (var result in q) {
				sink.Write("\n" + result.lr0.ToString("g4").PadRight(9) + "p" + result.lrP.ToString("g4").PadRight(9) + "b" + result.lrB.ToString("g4").PadRight(9) + ": "
						+ result.errs.training.ToString("g4").PadRight(9) + ";"
						+ result.errs.test.ToString("g4").PadRight(9) + ";"
						+ result.errs.nn.ToString("g4").PadRight(9) + "; [" + result.errs.cumLearningRate + "]"
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
		//new DirectoryInfo(@"D:\EamonLargeDocs\VersionControlled\docs-trunk\programs\LvqEmn\data\datasets\");
		public static LvqDatasetCli Load(int folds, string name, uint rngInst) {

			var dataFile = dataDir.GetFiles(name + ".data").FirstOrDefault();

			return LoadDatasetImpl.LoadData(dataFile, false, false, rngInst, folds, null);

			//var dataFile = dataDir.GetFiles(name + ".data").FirstOrDefault();
			//var testFile = dataDir.GetFiles(name.Replace("train", "test") + ".data").FirstOrDefault();

			//var dataset = LoadDatasetImpl.LoadData(dataFile, false, false, rngInst, testFile != null ? 0 : folds, testFile != null ? testFile.Name : null);
			//dataset.TestSet = testFile != null ? LoadDatasetImpl.LoadData(testFile, false, false, rngInst, 0, null) : null;

			//return dataset;
		}



		// ReSharper disable RedundantAssignment
		public static IEnumerable<LvqDatasetCli> Datasets(int folds, uint rngParam, uint rngInst) {
			yield return PlainDataset(folds, rngParam++, rngInst++, 16, 3);
			yield return PlainDataset(folds, rngParam++, rngInst++, 8, 3);
			//yield return PlainDataset(folds, rngParam++, rngInst++, 16, 3, 0.8);
			//yield return PlainDataset(folds, rngParam++, rngInst++, 10, 4, 1.5);//new
			yield return StarDataset(folds, rngParam++, rngInst++, 12, 4);
			yield return StarDataset(folds, rngParam++, rngInst++, 8, 3);
			//yield return StarDataset(folds, rngParam++, rngInst++, 24, 4);
			//yield return StarDataset(folds, rngParam++, rngInst++, 8, 2);//new
			yield return Load(folds, "segmentationNormed_combined", rngInst++);
			//yield return Load(folds, "segmentation_combined", rngInst++);
			yield return Load(folds, "colorado", rngInst++);
			yield return Load(folds, "pendigits.train", rngInst++);
			//yield return Load(folds, "segmentationNormed_combined", rngInst++);//new:different ordering!
			//yield return Load(folds, "segmentation_combined", rngInst++);
			//yield return Load(folds, "colorado", rngInst++);
			//yield return Load(folds, "pendigits.train", rngInst++);
			// ReSharper restore RedundantAssignment
		}

		public static void Run(TextWriter sink, LvqModelType modeltype, int protos, long itersToRun) {
			//big: param42,inst37; model: iter51, param51
			//mix: param42,inst37; model: iter52, param51
			//alt: param42,inst37; model: iter52, param52
			//base: 1,2
			using (new DTimer(time => sink.WriteLine("Search Complete!  Tookr " + time)))
				FindOptimalLr(sink, Datasets(10, 1000, 1001).ToArray(), itersToRun, modeltype, protos, rngIter, rngParams);
		}
		const int offset = 3;
		const int rngIter = 2000+2*offset;
		const int rngParams = 2001+2*offset;
		static readonly string rngName = "alt"+offset;

		//const int rngIter = 2002;
		//const int rngParams = 2003;
		//const string rngName = "alt";
	
		//const int rngIter = 2002;
		//const int rngParams = 2001;
		//const string rngName = "bPaI";

		//const int rngIter = 2004;
		//const int rngParams = 2005;
		//const string rngName = "alt2";
		
		//const int rngIter = 2004;
		//const int rngParams = 2001;
		//const string rngName = "bPa2I";


		public static void SaveLogFor(string shortname, string logcontents) {
			var logfilepath = 
				Enumerable.Range(0, 1000)
				.Select(i => shortname + rngName + (i == 0 ? "" : " (" + i + ")") + ".txt")
				.Select(filename => Path.Combine(resultsDir.FullName, filename))
				.Where(path => !File.Exists(path))
				.First();

			File.WriteAllText(logfilepath, logcontents);
		}
	}
}
