using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using EmnExtensions.Wpf;
using EmnExtensions.Wpf.Plot.VizEngines;
using LvqLibCli;
using EmnExtensions.MathHelpers;
using System.Threading.Tasks;
using System.Threading;
using EmnExtensions;

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

		public bool IsMultiModel { get { return ModelCount > 1; } }

		public double CurrentLearningRate { get { return subModels.Sum(model => model.UnscaledLearningRate) / ModelCount; } }

		public struct Statistic { public double[] Value, StandardError;}
		readonly object statCacheSync = new object();
		readonly List<Statistic> statCache = new List<Statistic>();
		public Statistic[] TrainingStats {
			get {
				var newstats = subModels.Select(m => m.GetTrainingStatsAfter(statCache.Count)).ToArray();
				int newStatCount = newstats.Min(statArray => statArray.Length);
				lock (statCacheSync) {
					for (int i = 0; i < newStatCount; ++i) {
						MeanVarCalc[] accum = null;

						foreach (var statArray in newstats)
							if (accum == null)
								accum = MeanVarCalc.ForValues(statArray[i].values);
							else
								MeanVarCalc.Add(accum, statArray[i].values);

						Statistic newStat = new Statistic { Value = new double[accum.Length], StandardError = new double[accum.Length], };
						for (int mi = 0; mi < accum.Length; ++mi) {
							newStat.Value[mi] = accum[mi].Mean;
							newStat.StandardError[mi] = Math.Sqrt(accum[mi].SampleVar / subModels.Length);
						}
						statCache.Add(newStat);
					}
					return statCache.ToArray();
				}
			}
		}
		const int ParWindow = 4;
		public bool FitsDataShape(LvqDatasetCli selectedDataset) { return subModels.First().FitsDataShape(selectedDataset); }
		readonly object epochsSynch = new object();
		int epochsDone;
		static int trainersRunning;
		public static void WaitForTraining() { while (trainersRunning != 0) Thread.Sleep(1); }
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


				//Parallel.ForEach(subModels, m => m.TrainUpto(epochsTarget,trainingSet,m.InitDataFold));

				while (epochsCurrent != epochsTarget) {
					epochsCurrent = (epochsTarget * 3 + epochsCurrent + 1) / 4;
					int currentTarget = epochsCurrent;
					foreach (var model in subModels)
						q.Add(Tuple.Create(model, currentTarget));
				}
				q.CompleteAdding();
				var helpers = Enumerable.Range(0, ParWindow).Select(ignored => Task.Factory.StartNew(() => { foreach (var next in q.GetConsumingEnumerable(cancel)) next.Item1.TrainUpto(next.Item2, trainingSet, next.Item1.InitDataFold); }, cancel, TaskCreationOptions.None, LowPriorityTaskScheduler.DefaultLowPriorityScheduler)).ToArray();
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

		public ModelProjection CurrentModelProjection(int subModelIdx, LvqDatasetCli dataset) {
			return subModels[subModelIdx].CurrentProjectionAndPrototypes(dataset);
		}

		public class ModelProjectionAndImage {
			public Point[][] PrototypesByLabel;
			public Point[][] PointsByLabel;
			public Rect Bounds;
			public int Width, Height;
			public uint[] ImageData;
			public LvqModels forModels;
			public int forSubModel;
			public LvqDatasetCli forDataset;
		}

		public ModelProjectionAndImage CurrentProjectionAndImage(int subModelIdx, LvqDatasetCli dataset, int width, int height) {
#if DEBUG
				int renderwidth = (width + 7) / 8;
				int renderheight = (height + 7) / 8;
#else
			int renderwidth = width;
			int renderheight = height;
#endif

			var selectedModel = subModels[subModelIdx];
			ModelProjection projection;
			Rect bounds;
			int[,] closestClass;
			lock (selectedModel.ReadSync) {
				projection = selectedModel.CurrentProjectionAndPrototypes(dataset);
				if(!projection.HasValue) return null;
				bounds = ComputeProjectionBounds(projection.Prototypes.Select(lp=>lp.point),projection.Points.Select(lp => lp.point));
				closestClass = selectedModel.ClassBoundaries(bounds.Left, bounds.Right, bounds.Bottom, bounds.Top, renderwidth, renderheight);
			}
			Debug.Assert(NotDefault(projection));
			Debug.Assert(NotDefault(bounds));
			Debug.Assert(NotDefault(closestClass));

			uint[] nativeColorsPerClass = NativeColorsPerClassAndBlack(dataset);
			uint[] boundaryImage = BoundaryImageFor(closestClass, nativeColorsPerClass, width, renderwidth, height, renderheight);
			return new ModelProjectionAndImage {
				Width = width,
				Height = height,
				ImageData = boundaryImage,
				Bounds = bounds,
				PrototypesByLabel = GroupPointsByLabel(projection.Prototypes, dataset.ClassCount),
				PointsByLabel = GroupPointsByLabel(projection.Points,dataset.ClassCount),
				forDataset = dataset,
				forModels = this,
				forSubModel = subModelIdx,
			};
		}

		static Point[][] GroupPointsByLabel(CliLvqLabelledPoint[] labelledPoints, int classCount) {
			//var projectedPointsByLabel = Enumerable.Range(0, dataPoints.Length).ToLookup(i => currProjection.Data.ClassLabels[i], i =>  Points.GetPoint(currProjection.Data.Points, i));
			int[] pointCountPerClass = new int[classCount];
			foreach (var p in labelledPoints) pointCountPerClass[p.label]++;

			Point[][] pointsByLabel = pointCountPerClass.Select(pointCount=> new Point[pointCount]).ToArray();
			int[] pointIndexPerClass = new int[classCount];
			for (int i = 0; i < labelledPoints.Length; ++i) {
				int label = labelledPoints[i].label;
				pointsByLabel[label][pointIndexPerClass[label]++] = labelledPoints[i].point;
			}
			return pointsByLabel;
		}

		struct IntPoint { public int X, Y;}
		static uint[] BoundaryImageFor(int[,] closestClass, uint[] nativeColorsPerClass,int width,int renderwidth,int height, int renderheight) {
			List<IntPoint> boundaryPoints = GetBoundaryPoints(closestClass);
			MakeBoundaryBlack(closestClass, boundaryPoints, nativeColorsPerClass.Length - 1);
			return ToNativeColorBmp(closestClass, nativeColorsPerClass, width,renderwidth,height,renderheight);
		}

		static uint[] ToNativeColorBmp(int[,] closestClass, uint[] nativeColorsPerClass, int width, int renderwidth, int height, int renderheight) {
			uint[] classboundaries = new uint[width * height];
			int px = 0;
			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
					classboundaries[px++] = nativeColorsPerClass[closestClass[y * renderheight / height, x * renderwidth / width]];

			return classboundaries;
		}

		static void MakeBoundaryBlack(int[,] closestClass, List<IntPoint> boundaryPoints, int blackIdx) {
			foreach (var coord in boundaryPoints)
				closestClass[coord.Y, coord.X] = blackIdx;
		}

		static List<IntPoint> GetBoundaryPoints(int[,] closestClass) {
			var edges = new List<IntPoint>();
			for (int y = 1; y < closestClass.GetLength(0) - 1; y++)
				for (int x = 1; x < closestClass.GetLength(1) - 1; x++) {
					if (closestClass[y, x] != closestClass[y, x - 1]
						|| closestClass[y, x] != closestClass[y, x + 1]
						|| closestClass[y, x] != closestClass[y - 1, x]
						|| closestClass[y, x] != closestClass[y + 1, x]
						)
						edges.Add(new IntPoint{ X=x,Y=y});
				}
			return edges;
		}

		static bool NotDefault<T>(T val) {
			return !Equals(val,default(T));
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
			VizPixelScatterHelpers.RecomputeBounds(cliLvqLabelledPoint.ToArray(), 0.99, 0.99, 20.0, out outer, out inner);
			inner.Union(VizPixelScatterHelpers.ComputeOuterBounds(prototypePositions.ToArray()));
			return inner;
		}

		public IEnumerable<LvqModelCli> SubModels { get { return subModels; } }
	}
}
