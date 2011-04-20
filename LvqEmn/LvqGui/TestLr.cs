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
			public double ErrorMean { get { return (training + test + nn) / 3.0; } }
			public override string ToString() {
				return TrainingControlValues.GetFormatted(training, trainingStderr) + "; " +
					TrainingControlValues.GetFormatted(test, testStderr) + "; " +
					TrainingControlValues.GetFormatted(nn, nnStderr) + "; ";
			}
		}

		public static ErrorRates ErrorOf(LvqDatasetCli[] dataset, int epochsToTrain, LvqModelType type, double lr0, double lrScaleP, double lrScaleB, uint rngIter, uint rngParam) {
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
					PrototypesPerClass = 5,
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
				model.Train(epochsToTrain, dataset[fold], fold);
				var stats = model.EvaluateStats(dataset[fold], fold);
				return stats;
			}
			).ToArray();

			var meanStats = LvqMultiModel.MeanStdErrStats(severalStats);
			//Console.Write(".");
			return new ErrorRates(meanStats, nnErrorIdx);
		}

		public static IEnumerable<double> LogRange(double start, double end, int steps) {
			//start*exp(ln(end/start) *  i/(steps-1) )
			double lnScale = Math.Log(end / start);
			for (int i = 0; i < steps; i++)
				yield return start * Math.Exp(lnScale * ((double)i / (steps - 1)));
		}

		const int splits = 10;

		public static void FindOptimalLr(LvqDatasetCli[] dataset,  int epochsToTrain, LvqModelType type, uint rngIter, uint rngParam) {
			var q =
				(from lr0 in LogRange(0.5, 0.005, splits).AsParallel()
				 from lrP in LogRange(1.0, 0.01, splits)
				 from lrB in (type == LvqModelType.GgmModelType || type == LvqModelType.G2mModelType ? LogRange(1.0, 0.01, splits) : new[] { 0.0 })
				 let errs = ErrorOf(dataset, epochsToTrain, type, lr0, lrP, lrB, rngIter, rngParam)
				 orderby errs.ErrorMean
				 select new { lr0, lrP, lrB, errs }).AsSequential();

			foreach (var result in q.Take(100)) {
				Console.WriteLine(result.lr0 + "/" + result.lrP + "/" + result.lrB + ": " + result.errs);
			}
		}

		public static LvqDatasetCli PlainDataset(int folds, uint rngParam, uint rngInst, int dims, int classes, double? classsep = null) {
			return LvqDatasetCli.ConstructGaussianClouds("simplemodel", folds, false, false, null, rngParam, rngInst, dims, classes, 10000 / dims, classsep ?? 1.5);
		}
		public static LvqDatasetCli StarDataset(int folds, uint rngParam, uint rngInst, int dims, int classes, double? starsep = null, double? classrelsep = null, double? sigmanoise = null) {
			return LvqDatasetCli.ConstructStarDataset("star", folds, false, false, null, rngParam, rngInst, dims, dims / 2, 3, classes, 10000 / dims, starsep ?? 1.5, classrelsep ?? 0.5, true, sigmanoise ?? 2.5);
		}
		static readonly DirectoryInfo dataDir = new DirectoryInfo(@"D:\EamonLargeDocs\VersionControlled\docs-trunk\programs\LvqEmn\data\datasets\");
		public static LvqDatasetCli Load(int folds, string name, uint rngInst) {
			var dataFile = dataDir.GetFiles(name + ".data").FirstOrDefault();
			var testFile = dataDir.GetFiles(name.Replace("train", "test") + ".data").FirstOrDefault();

			var dataset = LoadDatasetImpl.LoadData(dataFile, false, false, rngInst, folds, testFile != null ? testFile.Name : null);
			dataset.TestSet = testFile != null ? LoadDatasetImpl.LoadData(testFile, false, false, rngInst, folds, null) : null;

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
				FindOptimalLr(Datasets(10, 42, 37).ToArray(), 10, LvqModelType.GgmModelType, 51, 133);
		}
	}
}
