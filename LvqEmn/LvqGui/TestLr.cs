using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmnExtensions;
using EmnExtensions.DebugTools;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf;
using ExpressionToCodeLib;
using LvqLibCli;

namespace LvqGui {
	class TestLr {
		public struct ErrorRates {
			public readonly double training, trainingStderr, test, testStderr, nn, nnStderr;
			public ErrorRates(LvqMultiModel.Statistic stats, int nnIdx) {
				training = stats.Value[LvqTrainingStatCli.TrainingErrorI];
				test = stats.Value[LvqTrainingStatCli.TestErrorI];
				nn = stats.Value[nnIdx];
				trainingStderr = stats.StandardError[LvqTrainingStatCli.TrainingErrorI];
				testStderr = stats.StandardError[LvqTrainingStatCli.TestErrorI];
				nnStderr = stats.StandardError[nnIdx];
			}
			public double ErrorMean { get { return (training*3 + test + nn) / 5.0; } }
			public override string ToString() {
				return TrainingControlValues.GetFormatted(training, trainingStderr) + "; " +
					TrainingControlValues.GetFormatted(test, testStderr) + "; " +
					TrainingControlValues.GetFormatted(nn, nnStderr) + "; ";
			}
		}

		public static ErrorRates ErrorOf(LvqDatasetCli[] dataset, long iters, LvqModelType type, int protos, double lr0, double lrScaleP, double lrScaleB, uint rngIter, uint rngParam) {
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
					RngParamsSeed = rngIter + (uint)fold,
				});
				nnErrorIdx = model.TrainingStatNames.AsEnumerable().IndexOf(name => name.Contains("NN Error")); // threading irrelevant; all the same & atomic.
				model.Train((int)(iters / dataset[fold].PointCount), dataset[fold], fold);
				var stats = model.EvaluateStats(dataset[fold], fold);
				return stats;
			}
			).ToArray();

			var meanStats = LvqMultiModel.MeanStdErrStats(severalStats);
			Console.Write(".");
			return new ErrorRates(meanStats, nnErrorIdx);
		}

		public static IEnumerable<double> LogRange(double start, double end, int steps) {
			//start*exp(ln(end/start) *  i/(steps-1) )
			double lnScale = Math.Log(end / start);
			for (int i = 0; i < steps; i++)
				yield return start * Math.Exp(lnScale * ((double)i / (steps - 1)));
		}

		public static void FindOptimalLr(LvqDatasetCli[] dataset, long iters, LvqModelType type, int protos, uint rngIter, uint rngParam) {
			var lr0range = LogRange(0.3, 0.01, 4);
			var lrPrange = LogRange(0.5, 0.03, 4);
			var lrBrange = (type == LvqModelType.GgmModelType || type == LvqModelType.G2mModelType ? LogRange(0.1, 0.003, 4) : new[] { 0.0 });

			var q =
				(from lr0 in lr0range.AsParallel()
				 from lrP in lrPrange
				 from lrB in lrBrange
				 let errs = ErrorOf(dataset, iters, type, protos, lr0, lrP, lrB, rngIter, rngParam)
				 orderby errs.ErrorMean
				 select new { lr0, lrP, lrB, errs }).AsSequential();

			Console.WriteLine("lr0range:" + ObjectToCode.ComplexObjectToPseudoCode(lr0range));
			Console.WriteLine("lrPrange:" + ObjectToCode.ComplexObjectToPseudoCode(lrPrange));
			Console.WriteLine("lrBrange:" + ObjectToCode.ComplexObjectToPseudoCode(lrBrange));
			Console.WriteLine("For " + type + " with " + protos + " prototypes and " + iters + " iters training:");

			foreach (var result in q.Take(100)) {
				Console.Write("\n" + result.lr0.ToString("g4").PadRight(9) + "p" + result.lrP.ToString("g4").PadRight(9) + "b" + result.lrB.ToString("g4").PadRight(9) + ": "
						+ result.errs.training.ToString("g4").PadRight(9) + ";"
						+ result.errs.test.ToString("g4").PadRight(9) + ";"
						+ result.errs.nn.ToString("g4").PadRight(9) + ";"
					);
			}
			Console.WriteLine();
		}

		public static LvqDatasetCli PlainDataset(int folds, uint rngParam, uint rngInst, int dims, int classes, double? classsep = null) {
			return LvqDatasetCli.ConstructGaussianClouds("simplemodel", folds, false, false, null, rngParam, rngInst, dims, classes, (int)(10000 / Math.Sqrt(dims) / classes), classsep ?? 1.5);
		}
		public static LvqDatasetCli StarDataset(int folds, uint rngParam, uint rngInst, int dims, int classes, double? starsep = null, double? classrelsep = null, double? sigmanoise = null) {
			return LvqDatasetCli.ConstructStarDataset("star", folds, false, false, null, rngParam, rngInst, dims, dims / 2, 3, classes, (int)(10000 / Math.Sqrt(dims) / classes), starsep ?? 1.5, classrelsep ?? 0.5, true, sigmanoise ?? 2.5);
		}
		static readonly DirectoryInfo dataDir = new DirectoryInfo(@"D:\EamonLargeDocs\VersionControlled\docs-trunk\programs\LvqEmn\data\datasets\");
		public static LvqDatasetCli Load(int folds, string name, uint rngInst) {
			var dataFile = dataDir.GetFiles(name + ".data").FirstOrDefault();
			var testFile = dataDir.GetFiles(name.Replace("train", "test") + ".data").FirstOrDefault();

			var dataset = LoadDatasetImpl.LoadData(dataFile, false, false, rngInst, testFile != null ? 0 : folds, testFile != null ? testFile.Name : null);
			dataset.TestSet = testFile != null ? LoadDatasetImpl.LoadData(testFile, false, false, rngInst, 0, null) : null;

			return dataset;
		}

		public static IEnumerable<LvqDatasetCli> Datasets(int folds, uint rngParam, uint rngInst) {
			yield return PlainDataset(folds, rngParam++, rngInst++, 16, 3);
			yield return PlainDataset(folds, rngParam++, rngInst++, 8, 3);
			yield return PlainDataset(folds, rngParam++, rngInst++, 16, 3, 0.8);
			yield return StarDataset(folds, rngParam++, rngInst++, 8, 3);
			yield return StarDataset(folds, rngParam++, rngInst++, 16, 3);
			yield return StarDataset(folds, rngParam++, rngInst++, 24, 4);
			yield return Load(folds, "segmentationNormed_combined", rngInst++);
			yield return Load(folds, "segmentation_combined", rngInst++);
			yield return Load(folds, "colorado", rngInst++);
			yield return Load(folds, "pendigits.train", rngInst++);
		}

		public static void Run() {
			using (var proc = Process.GetCurrentProcess())
				proc.PriorityClass = ProcessPriorityClass.BelowNormal;
			using (new DTimer("search"))
				FindOptimalLr(Datasets(10, 42, 37).ToArray(), 10000000, LvqModelType.GmModelType, 1, 51, 133);
		}
	}
}
