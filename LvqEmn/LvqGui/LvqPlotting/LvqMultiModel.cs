using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using EmnExtensions;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf;
using EmnExtensions.Wpf.Plot.VizEngines;
using LvqLibCli;

namespace LvqGui {
	public class LvqMultiModel {
		readonly LvqModelCli[] subModels;
		public LvqMultiModel(LvqDatasetCli forDataset, LvqModelSettingsCli lvqModelSettingsCli) {
			string shorthand = lvqModelSettingsCli.ToShorthand() + "--" + forDataset.DatasetLabel;
			subModels =
				Enumerable.Range(0, lvqModelSettingsCli.ParallelModels).AsParallel()
				.Select(modelfold => new LvqModelCli(shorthand, forDataset, modelfold, lvqModelSettingsCli, true))
				.OrderBy(model => model.InitDataFold)
				.ToArray();
		}

		public string ModelLabel { get { return subModels.First().ModelLabel; } }

		public int ModelCount { get { return subModels.Length; } }

		public LvqDatasetCli InitSet { get { return subModels.First().InitDataset; } }

		public bool IsProjectionModel { get { return subModels.First().IsProjectionModel; } }

		public string[] TrainingStatNames { get { return subModels.First().TrainingStatNames; } }

		public bool IsMultiModel { get { return ModelCount > 1; } }

		public double CurrentLearningRate { get { return subModels.Sum(model => model.UnscaledLearningRate) / ModelCount; } }

		public int SelectedSubModel { get; set; }

		public struct Statistic { public double[] Value, StandardError; public int BestIdx;}


		public Statistic EvaluateStats(LvqDatasetCli selectedDataset) { return MeanStdErrStats(EvaluateFullStats(selectedDataset)); }
		public IEnumerable<LvqTrainingStatCli> EvaluateFullStats(LvqDatasetCli selectedDataset) { return subModels.Select(m => m.EvaluateStats(selectedDataset, m.InitDataFold)); }
		public int GetBestSubModelIdx(LvqDatasetCli selectedDataset) { return MinIdx(EvaluateFullStats(selectedDataset).Select(stat => stat.values[LvqTrainingStatCli.TrainingErrorI])); }

		public string CurrentStatsString(LvqDatasetCli selectedDataset) {
			var meanstats = EvaluateStats(selectedDataset);
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < TrainingStatNames.Length; i++)
				sb.AppendLine(TrainingStatNames[i].Split('!')[0] + ": " + Statistics.GetFormatted(meanstats.Value[i], meanstats.StandardError[i]));
			sb.AppendLine("Best idx: " + meanstats.BestIdx);

			return sb.ToString();
		}
		public string CurrentFullStatsString(LvqDatasetCli selectedDataset) {
			var allstats = EvaluateFullStats(selectedDataset).ToArray();
			var meanstats = MeanStdErrStats(allstats);

			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < TrainingStatNames.Length; i++)
				sb.AppendLine(TrainingStatNames[i].Split('!')[0] + ": " + string.Join(", ", allstats.Select(stats => Statistics.GetFormatted(stats.values[i], meanstats.StandardError[i], 0, true))));
			sb.AppendLine("Best idx: " + meanstats.BestIdx);

			return sb.ToString();
		}

		public LvqTrainingStatCli[] SelectedStats(int submodel) { return subModels[submodel].TrainingStats; }
		readonly object statCacheSync = new object();
		readonly List<Statistic> statCache = new List<Statistic>();
		int statProcIdx;
		public Statistic[] TrainingStats {
			get {
				lock (statCacheSync) {
					var newstats = subModels.Select(m => m.GetTrainingStatsAfter(statProcIdx)).ToArray();
					int newStatCount = newstats.Min(statArray => statArray == null ? 0 : statArray.Length);
					statProcIdx += newStatCount;
					for (int i = 0; i < newStatCount; ++i)
						statCache.Add(MeanStdErrStats(newstats.Select(modelstats => modelstats[i])));
					while (statCache.Count > 512) {
						//Console.WriteLine("Trimming from " + statCache.Count);
						for (int i = 1; i < 256; i++) statCache[i] = statCache[2 * i];
						for (int i = 512; i < statCache.Count; i++) statCache[i - 256] = statCache[i];
						statCache.RemoveRange(statCache.Count - 256, 256);
					}
					return statCache.ToArray();
				}
			}
		}

		public static Statistic MeanStdErrStats(IEnumerable<LvqTrainingStatCli> newstats) {
			MeanVarCalc[] accum = null;

			foreach (var statArray in newstats)
				if (accum == null)
					accum = MeanVarCalc.ForValues(statArray.values);
				else
					MeanVarCalc.Add(accum, statArray.values);


			Statistic newStat = new Statistic { Value = new double[accum.Length], StandardError = new double[accum.Length], BestIdx = MinIdx(newstats.Select(stat => stat.values[LvqTrainingStatCli.TrainingErrorI])) };
			for (int mi = 0; mi < accum.Length; ++mi) {
				newStat.Value[mi] = accum[mi].Mean;
				newStat.StandardError[mi] = Math.Sqrt(accum[mi].SampleVar / accum[mi].Weight);
			}
			return newStat;
		}

		static int MinIdx(IEnumerable<double> vals) {
			int minidx = -1;
			double minval = double.PositiveInfinity;
			int idx = 0;
			foreach (double val in vals) {
				if (val < minval) {
					minval = val;
					minidx = idx;
				}
				idx++;
			}
			return minidx;
		}

		static readonly int ParWindow = Environment.ProcessorCount;
		public bool FitsDataShape(LvqDatasetCli selectedDataset) { return subModels.First().FitsDataShape(selectedDataset); }
		readonly object epochsSynch = new object();
		int epochsDone;
		static int trainersRunning;
		public static void WaitForTraining() { while (trainersRunning != 0) Thread.Sleep(1); }
		public void Train(int epochsToDo, LvqDatasetCli trainingSet, CancellationToken cancel) {
			if (cancel.IsCancellationRequested) return;
			int epochsTarget;
			lock (epochsSynch)
				epochsTarget = epochsDone += epochsToDo;
			TrainImpl(cancel, epochsTarget - epochsToDo, epochsTarget, trainingSet);
		}
		public void TrainUpto(int epochsToTrainUpto, LvqDatasetCli trainingSet, CancellationToken cancel) {
			if (cancel.IsCancellationRequested) return;
			int epochsCurrent;
			lock (epochsSynch) {
				if (epochsDone >= epochsToTrainUpto)
					return;
				epochsCurrent = epochsDone;
				epochsDone = epochsToTrainUpto;
			}
			TrainImpl(cancel, epochsCurrent, epochsToTrainUpto, trainingSet);
		}

		void TrainImpl(CancellationToken cancel, int epochsCurrent, int epochsTarget, LvqDatasetCli trainingSet) {

			Interlocked.Increment(ref trainersRunning);
			try {
				var trainingqueue = new BlockingCollection<Tuple<LvqModelCli, int>>();

				while (epochsCurrent != epochsTarget) {
					epochsCurrent += ((epochsTarget - epochsCurrent) + 1) / 2;
					int currentTarget = epochsCurrent;
					foreach (var model in subModels)
						trainingqueue.Add(Tuple.Create(model, currentTarget));
				}
				trainingqueue.CompleteAdding();
				var helpers = Enumerable.Range(0, Math.Min(ParWindow, ModelCount))
					.Select(ignored =>
							Task.Factory.StartNew(
								() => {
									foreach (var next in trainingqueue.GetConsumingEnumerable(cancel))
										next.Item1.TrainUpto(next.Item2, trainingSet, next.Item1.InitDataFold);
								},
								cancel, TaskCreationOptions.None, LowPriorityTaskScheduler.DefaultLowPriorityScheduler)
					).ToArray();
				Task.WaitAll(helpers, cancel);
			} finally {
				Interlocked.Decrement(ref trainersRunning);
			}
		}

		public void ResetLearningRate() {
			foreach (var model in subModels)
				model.ResetLearningRate();
		}

		public MatrixContainer<byte> ClassBoundaries(int subModelIdx, double x0, double x1, double y0, double y1, int xCols, int yRows) {
			return subModels[subModelIdx].ClassBoundaries(x0, x1, y0, y1, xCols, yRows);
		}

		public ModelProjection CurrentModelProjection(int subModelIdx, LvqDatasetCli dataset) {
			return subModels[subModelIdx].CurrentProjectionAndPrototypes(dataset);
		}

		public class ModelProjectionAndImage {
			public Point[][] PrototypesByLabel;
			public Point[][] PointsByLabel;
			public Rect Bounds;
			public int Width, Height;
			public uint[] ImageData;
			public LvqMultiModel forModels;
			public int forSubModel;
			public LvqDatasetCli forDataset;
		}

		public ModelProjectionAndImage CurrentProjectionAndImage(LvqDatasetCli dataset, int width, int height, bool hideBoundaries, int currSubModel) {
#if DEBUG
			int renderwidth = (width + 7) / 8;
			int renderheight = (height + 7) / 8;
#else
			int renderwidth = width;
			int renderheight = height;
#endif
			var selectedModel = subModels[currSubModel];
			ModelProjection projection;
			Rect bounds;
			MatrixContainer<byte> closestClass;
			lock (selectedModel.ReadSync) {
				projection = selectedModel.CurrentProjectionAndPrototypes(dataset);
				if (!projection.HasValue) return null;
				bounds = ExpandToShape(renderwidth, renderheight, ComputeProjectionBounds(projection.Prototypes.Select(lp => lp.point), projection.Points.Select(lp => lp.point)));
				if (!hideBoundaries)
					closestClass = selectedModel.ClassBoundaries(bounds.Left, bounds.Right, bounds.Bottom, bounds.Top, renderwidth, renderheight);
				else
					closestClass = default(MatrixContainer<byte>);
			}
			Debug.Assert(NotDefault(projection));
			Debug.Assert(NotDefault(bounds));

			uint[] nativeColorsPerClass = NativeColorsPerClassAndBlack(dataset);
			uint[] boundaryImage = closestClass.IsSet() ? BoundaryImageFor(closestClass, nativeColorsPerClass, width, renderwidth, height, renderheight) : null;
			return new ModelProjectionAndImage {
				Width = width,
				Height = height,
				ImageData = boundaryImage,
				Bounds = bounds,
				PrototypesByLabel = GroupPointsByLabel(projection.Prototypes, dataset.ClassCount),
				PointsByLabel = GroupPointsByLabel(projection.Points, dataset.ClassCount),
				forDataset = dataset,
				forModels = this,
				forSubModel = currSubModel,
			};
		}

		static Rect ExpandToShape(int width, int height, Rect rect) {
			width = Math.Max(width, 1);
			height = Math.Max(height, 1);
			if (rect.Width / width < rect.Height / height) {
				double scale = (rect.Height / height) / (rect.Width / width);
				return new Rect(rect.X - rect.Width * (scale - 1) / 2, rect.Y, rect.Width * scale, rect.Height);
			} else {
				double scale = (rect.Width / width) / (rect.Height / height);
				return new Rect(rect.X, rect.Y - rect.Height * (scale - 1) / 2, rect.Width, rect.Height * scale);
			}
		}

		static Point[][] GroupPointsByLabel(CliLvqLabelledPoint[] labelledPoints, int classCount) {
			//var projectedPointsByLabel = Enumerable.Range(0, dataPoints.Length).ToLookup(i => currProjection.Data.ClassLabels[i], i =>  Points.GetPoint(currProjection.Data.Points, i));
			int[] pointCountPerClass = new int[classCount];
			foreach (var p in labelledPoints) pointCountPerClass[p.label]++;

			Point[][] pointsByLabel = pointCountPerClass.Select(pointCount => new Point[pointCount]).ToArray();
			int[] pointIndexPerClass = new int[classCount];
			for (int i = 0; i < labelledPoints.Length; ++i) {
				int label = labelledPoints[i].label;
				pointsByLabel[label][pointIndexPerClass[label]++] = labelledPoints[i].point;
			}
			return pointsByLabel;
		}

		struct IntPoint { public int X, Y;}
		static uint[] BoundaryImageFor(MatrixContainer<byte> closestClass, uint[] nativeColorsPerClass, int width, int renderwidth, int height, int renderheight) {
			List<IntPoint> boundaryPoints = GetBoundaryPoints(closestClass);
			MakeBoundaryBlack(closestClass, boundaryPoints, (byte)(nativeColorsPerClass.Length - 1));
			return ToNativeColorBmp(closestClass, nativeColorsPerClass, width, renderwidth, height, renderheight);
		}

		static uint[] ToNativeColorBmp(MatrixContainer<byte> closestClass, uint[] nativeColorsPerClass, int width, int renderwidth, int height, int renderheight) {
			uint[] classboundaries = new uint[width * height];
			int px = 0;
			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
					classboundaries[px++] = nativeColorsPerClass[closestClass[y * renderheight / height, x * renderwidth / width]];

			return classboundaries;
		}

		static void MakeBoundaryBlack(MatrixContainer<byte> closestClass, List<IntPoint> boundaryPoints, byte blackIdx) {
			foreach (var coord in boundaryPoints)
				closestClass.Set(coord.Y, coord.X, blackIdx);
		}

		static List<IntPoint> GetBoundaryPoints(MatrixContainer<byte> closestClass) {
			var edges = new List<IntPoint>();
			for (int y = 1; y < closestClass.rows - 1; y++)
				for (int x = 1; x < closestClass.cols - 1; x++) {
					int addr = closestClass.cols * y + x;
					var val = closestClass.arr[addr];
					if (val != closestClass.arr[addr - 1]
						|| val != closestClass.arr[addr + 1]
						|| val != closestClass.arr[addr - closestClass.cols]
						|| val != closestClass.arr[addr + closestClass.cols]
						)
						edges.Add(new IntPoint { X = x, Y = y });
				}
			return edges;
		}

		static bool NotDefault<T>(T val) {
			return !Equals(val, default(T));
		}

		static uint[] NativeColorsPerClassAndBlack(LvqDatasetCli dataset) {
			return dataset.ClassColors
				.Select(c => { c.ScA = 0.05f; return c; })
				.Concat(Enumerable.Repeat(Color.FromRgb(0, 0, 0), 1))
				.Select(c => c.ToNativeColor())
				.ToArray();
		}


		static Rect ComputeProjectionBounds(IEnumerable<Point> prototypePositions, IEnumerable<Point> cliLvqLabelledPoint) {
			Rect outer, inner;
			VizPixelScatterHelpers.RecomputeBounds(cliLvqLabelledPoint.ToArray(), 0.95, 0.95, 5.0, out outer, out inner);
			//			inner.Union(VizPixelScatterHelpers.ComputeOuterBounds(prototypePositions.ToArray()));
			return inner;
		}

		public IEnumerable<LvqModelCli> SubModels { get { return subModels; } }

		public object Tag { get; set; }

	}
}
