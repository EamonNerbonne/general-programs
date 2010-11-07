using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LvqLibCli;
using EmnExtensions.MathHelpers;
using System.Threading.Tasks;
using System.Threading;

namespace LvqGui {
	public class LvqModels {
		readonly LvqModelCli[] subModels;
		public LvqModels(string shorthand, int parallelModels, LvqDatasetCli forDataset, LvqModelSettingsCli lvqModelSettingsCli) {
			subModels =
				Enumerable.Range(0, parallelModels)
				.Select(datafold => new LvqModelCli(shorthand, forDataset, datafold, lvqModelSettingsCli))
				.ToArray();
		}

		public string ModelLabel { get { return subModels.First().ModelLabel; } }

		public int ModelCount { get { return subModels.Length; } }

		public LvqDatasetCli InitSet { get { return subModels.First().InitDataset; } }

		public bool IsProjectionModel { get { return subModels.First().IsProjectionModel; } }

		public IEnumerable<string> TrainingStatNames { get { return subModels.First().TrainingStatNames; } }

		public bool IsMultiModel { get { return ModelCount > 0; } }

		public double CurrentLearningRate { get { return subModels.Sum(model => model.CurrentLearningRate) / ModelCount; } }

		public struct Statistic { public double[] Value, StandardError;}
		readonly object statCacheSync = new object();
		readonly List<Statistic> statCache = new List<Statistic>();
		public Statistic[] TrainingStats {
			get {
				int statCount = subModels.Min(model => model.TrainingStatCount);
				lock (statCacheSync) {
					while (statCount > statCache.Count) {
						MeanVarCalc[] accum = null;

						foreach (var model in subModels) {
							if (accum == null)
								accum = MeanVarCalc.ForValues(model.GetTrainingStat(statCache.Count).values);
							else
								MeanVarCalc.Add(accum, model.GetTrainingStat(statCache.Count).values);
						}
						Statistic newStat = new Statistic { Value = new double[accum.Length], StandardError = new double[accum.Length], };
						for (int i = 0; i < accum.Length; ++i) {
							newStat.Value[i] = accum[i].Mean;
							newStat.StandardError[i] = Math.Sqrt(accum[i].SampleVar / subModels.Length);
						}
						statCache.Add(newStat);
					}
					return statCache.ToArray();
				}
			}
		}
		const int ParWindow = 8;
		public bool FitsDataShape(LvqDatasetCli selectedDataset) { return subModels.First().FitsDataShape(selectedDataset); }
		readonly object epochsSynch = new object();
		int epochsDone;
		static int trainersRunning;
		public static void WaitForTraining() { while (trainersRunning != 0)Thread.Sleep(1); }
		public void Train(int epochsToDo, LvqDatasetCli trainingSet, CancellationToken cancel) {
			Interlocked.Increment(ref trainersRunning);
			try {
				if (cancel.IsCancellationRequested) return;
				int epochsCurrent;
				int epochsTarget;
				lock (epochsSynch) {
					epochsCurrent = epochsDone;
					epochsDone += epochsToDo;
					epochsTarget = epochsDone;
				}
				BlockingCollection<Tuple<LvqModelCli, int>> q = new BlockingCollection<Tuple<LvqModelCli, int>>();

				var helpers = subModels.Select(m => Task.Factory.StartNew(() => { foreach (var next in q.GetConsumingEnumerable(cancel)) next.Item1.TrainUpto(next.Item2, trainingSet, next.Item1.InitDataFold); }, cancel)).ToArray();


				while (epochsCurrent != epochsTarget) {
					epochsCurrent = (epochsTarget + epochsCurrent + 1) / 2;
					int currentTarget = epochsCurrent;
					foreach (var model in subModels)
						q.Add(Tuple.Create(model, currentTarget));
				}
				q.CompleteAdding();
				Task.WaitAll(helpers, cancel);
			} finally {
				Interlocked.Decrement(ref trainersRunning);
			}
		}

		public void ResetLearningRate() {
			foreach (var model in subModels)
				model.ResetLearningRate();
		}

		public int[,] ClassBoundaries(int subModelIdx, double x0, double x1, double y0, double y1, int xCols, int yRows) {
			return subModels[subModelIdx].ClassBoundaries(x0, x1, y0, y1, xCols, yRows);
		}

		public ModelProjection CurrentProjectionAndPrototypes(int subModelIdx, LvqDatasetCli dataset) {
			return subModels[subModelIdx].CurrentProjectionAndPrototypes(dataset);
		}

		public IEnumerable<LvqModelCli> SubModels { get { return subModels; } }
	}
}
