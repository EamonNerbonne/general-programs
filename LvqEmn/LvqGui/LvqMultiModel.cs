using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using EmnExtensions;
using EmnExtensions.Filesystem;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf;
using EmnExtensions.Wpf.Plot;
using EmnExtensions.Wpf.Plot.VizEngines;
using LvqGui.CreatorGui;
using LvqGui.LvqPlotting;
using LvqLibCli;

namespace LvqGui
{
    public sealed class LvqMultiModel
    {
        readonly LvqModelCli[] subModels;
        public readonly LvqModelSettingsCli OriginalSettings;

        public LvqMultiModel(LvqDatasetCli forDataset, LvqModelSettingsCli lvqModelSettingsCli, bool trackStats = true)
        {
            if (lvqModelSettingsCli.LR0 == 0.0 || lvqModelSettingsCli.LrScaleP == 0.0) {
                throw new ArgumentException("Suspicious settings with 0 learning rate: " + lvqModelSettingsCli.ToShorthand());
            }

            OriginalSettings = lvqModelSettingsCli;
            var shorthand = lvqModelSettingsCli.ToShorthand() + "--" + forDataset.DatasetLabel;
            subModels =
                Enumerable.Range(0, lvqModelSettingsCli.ParallelModels).AsParallel()
                    .Select(modelfold => new LvqModelCli(shorthand, forDataset, modelfold + lvqModelSettingsCli.FoldOffset, lvqModelSettingsCli, trackStats))
                    .OrderBy(model => model.DataFold)
                    .ToArray();
            nnErrIdx = subModels[0].TrainingStatNames.AsEnumerable().IndexOf(name => name.Contains("NN Error"));
        }

        public readonly int nnErrIdx;

        public string ModelLabel
            => subModels.First().ModelLabel;

        public int ModelCount
            => subModels.Length;

        public LvqDatasetCli InitSet
            => subModels.First().TrainingSet;

        public bool IsProjectionModel
            => subModels.First().IsProjectionModel;

        public string[] TrainingStatNames
            => subModels.First().TrainingStatNames;

        public bool IsMultiModel
            => ModelCount > 1;

        public double CurrentMeanLearningRate
            => subModels.Sum(model => model.MeanUnscaledLearningRate) / ModelCount;

        public int SelectedSubModel { get; set; }

        public struct Statistic
        {
            public double[] Value, StandardError;
            public int BestIdx;
        }

        public static double GetItersPerEpoch(LvqDatasetCli dataset, int fold)
            => dataset.PointCount(fold);

        public Statistic CurrentRawStats()
            => MeanStdErrStats(EvaluateFullStats());

        public IEnumerable<LvqTrainingStatCli> EvaluateFullStats()
            => subModels.Select(m => Task.Factory.StartNew(() => m.EvaluateStats())).ToArray().Select(t => t.Result);

        public int GetBestSubModelIdx()
            => MinIdx(EvaluateFullStats().Select(stat => stat.values[LvqTrainingStatCli.TrainingErrorI]));

        public string CurrentStatsString()
        {
            var meanstats = CurrentRawStats();
            var sb = new StringBuilder();
            for (var i = 0; i < TrainingStatNames.Length; i++) {
                _ = sb.AppendLine(TrainingStatNames[i].Split('!')[0] + ": " + Statistics.GetFormatted(meanstats.Value[i], meanstats.StandardError[i]));
            }

            _ = sb.AppendLine("Best idx: " + meanstats.BestIdx);

            return sb.ToString();
        }

        public string CurrentFullStatsString()
        {
            var allstats = EvaluateFullStats().ToArray();
            return FullStatsString(allstats);
        }

        string FullStatsString(LvqTrainingStatCli[] allstats)
        {
            var meanstats = MeanStdErrStats(allstats);

            var sb = new StringBuilder();
            for (var i = 0; i < TrainingStatNames.Length; i++) {
                _ = sb.AppendLine(TrainingStatNames[i].Split('!')[0] + ": " + string.Join(", ", allstats.Select(stats => Statistics.GetFormatted(stats.values[i], meanstats.StandardError[i], 0, true))));
            }

            _ = sb.AppendLine("Best idx: " + meanstats.BestIdx);

            return sb.ToString();
        }

        public LvqTrainingStatCli[] SelectedStats(int submodel)
            => subModels[submodel].TrainingStats;

        readonly object statCacheSync = new();
        readonly List<Statistic> statCache = new();
        Statistic[] cachedStatCache = Array.Empty<Statistic>();
        int statProcIdx;

        public Statistic[] TrainingStats
        {
            get {
                lock (statCacheSync) {
                    var newstats = subModels.Select(m => m.GetTrainingStatsAfter(statProcIdx)).ToArray();
                    var newStatCount = newstats.Min(statArray => statArray?.Length ?? 0);
                    if (newStatCount == 0) {
                        return cachedStatCache;
                    }

                    statProcIdx += newStatCount;
                    for (var i = 0; i < newStatCount; ++i) {
                        statCache.Add(MeanStdErrStats(newstats.Select(modelstats => modelstats[i])));
                    }

                    while (statCache.Count > 256) {
                        //Console.WriteLine("Trimming from " + statCache.Count);
                        for (var i = 1; i < 128; i++) {
                            statCache[i] = statCache[2 * i];
                        }

                        for (var i = 256; i < statCache.Count; i++) {
                            statCache[i - 128] = statCache[i];
                        }

                        statCache.RemoveRange(statCache.Count - 128, 128);
                    }

                    return cachedStatCache = statCache.ToArray();
                }
            }
        }

        public static Statistic MeanStdErrStats(IEnumerable<LvqTrainingStatCli> newstats)
        {
            var statDistribution =
                newstats.Aggregate(
                    default(MeanVarDistrib[]),
                    (acc, statArray) =>
                        acc == null
                            ? statArray.values.Select(val => MeanVarDistrib.Init(val)).ToArray()
                            : acc.Zip(statArray.values, (mv, val) => mv.Add(val)).ToArray()
                );

            var newStat = new Statistic { Value = new double[statDistribution.Length], StandardError = new double[statDistribution.Length], BestIdx = MinIdx(newstats.Select(stat => stat.values[LvqTrainingStatCli.TrainingErrorI])) };
            for (var mi = 0; mi < statDistribution.Length; ++mi) {
                newStat.Value[mi] = statDistribution[mi].Mean;
                newStat.StandardError[mi] = Math.Sqrt(statDistribution[mi].SampleVar / statDistribution[mi].Weight);
            }

            return newStat;
        }

        static int MinIdx(IEnumerable<double> vals)
        {
            var minidx = -1;
            var minval = double.PositiveInfinity;
            var idx = 0;
            foreach (var val in vals) {
                if (val < minval) {
                    minval = val;
                    minidx = idx;
                }

                idx++;
            }

            return minidx;
        }

        static readonly int ParWindow = Environment.ProcessorCount * 2;

        public bool FitsDataShape(LvqDatasetCli selectedDataset)
            => subModels.First().FitsDataShape(selectedDataset);

        //readonly object epochsSynch = new object();
        volatile int epochsDone;
        static int trainersRunning;

        public static void WaitForTraining()
        {
            while (trainersRunning != 0) {
                Thread.Sleep(1);
            }
        }

        public void TrainAndPrintOrder(CancellationToken cancel)
        {
            if (cancel.IsCancellationRequested) {
                return;
            }

            var selectedSubModel = SelectedSubModel;
            var helpers = subModels
                .Select(
                    (model, modelIndex) =>
                        Task.Factory.StartNew(
                            () => model.Train(1, modelIndex == selectedSubModel, false),
                            cancel,
                            TaskCreationOptions.None,
                            LowPriorityTaskScheduler.DefaultLowPriorityScheduler
                        )
                ).ToArray();
            var labelOrdering = Task.Factory.ContinueWhenAll(helpers, tasks => tasks.Select(task => task.Result).Single(labelOrder => labelOrder != null)).Result;
            Console.WriteLine(string.Join("", labelOrdering.Select(i => (char)(i < 10 ? i + '0' : i - 10 + 'a'))));
        }

        public void SortedTrain(CancellationToken cancel)
        {
            if (cancel.IsCancellationRequested) {
                return;
            }

            var helpers = subModels
                .Select(
                    model =>
                        Task.Factory.StartNew(
                            () => model.Train(1, false, true),
                            cancel,
                            TaskCreationOptions.None,
                            LowPriorityTaskScheduler.DefaultLowPriorityScheduler
                        )
                ).ToArray();
            Task.WaitAll(helpers, CancellationToken.None);
        }

        public void TrainEpochs(int epochsToDo, CancellationToken cancel)
        {
            if (cancel.IsCancellationRequested) {
                return;
            }

            var epochsTarget = Interlocked.Add(ref epochsDone, epochsToDo);
            TrainImpl(epochsTarget - epochsToDo, epochsTarget, cancel);
        }

        public void TrainUptoIters(double itersToTrainUpto, CancellationToken cancel)
            => TrainUptoEpochs((int)(itersToTrainUpto / GetItersPerEpoch(InitSet, 0) + 0.5), cancel);

        public void TrainUptoEpochs(int epochsToTrainUpto, CancellationToken cancel)
        {
            if (cancel.IsCancellationRequested) {
                return;
            }

            var epochsCurrent = epochsDone;
            while (true) {
                if (epochsCurrent >= epochsToTrainUpto) {
                    return;
                }

                var prevEpochs = Interlocked.CompareExchange(ref epochsDone, epochsToTrainUpto, epochsCurrent);
                if (prevEpochs == epochsCurrent) {
                    break; //successfully swapped;
                }

                epochsCurrent = prevEpochs;
            }

            TrainImpl(epochsCurrent, epochsToTrainUpto, cancel);
        }

        static readonly BlockingCollection<string> toLog = new();

        static LvqMultiModel()
            => new Thread(
                () => {
                    using var stream = File.Open(
                        Path.Combine(FSUtil.FindDataDir("lvqlog").FullName + "\\", "training-" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture) + ".log"),
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.Read
                    );
                    using var writer = new StreamWriter(stream, Encoding.UTF8);
                    foreach (var line in toLog.GetConsumingEnumerable()) {
                        writer.WriteLine(line);
                        if (toLog.Count == 0) {
                            writer.Flush();
                        }
                    }
                }
            ) { Priority = ThreadPriority.AboveNormal, IsBackground = true }.Start();

        void TrainImpl(int epochsCurrent, int epochsTarget, CancellationToken cancel)
        {
            _ = Interlocked.Increment(ref trainersRunning);
            try {
                var trainingqueue = new BlockingCollection<(LvqModelCli model, int epochsToTrainTo)>();
                while (epochsCurrent < epochsTarget) {
                    epochsCurrent += (epochsTarget - epochsCurrent + 1) / 2;
                    foreach (var model in subModels) {
                        trainingqueue.Add((model, epochsCurrent), cancel);
                    }
                }

                trainingqueue.CompleteAdding();
                var helpers = Enumerable.Range(0, Math.Min(subModels.Length, ParWindow))
                    .Select(
                        ignored =>
                            Task.Factory.StartNew(
                                () => {
                                    foreach (var next in trainingqueue.GetConsumingEnumerable(cancel)) {
                                        toLog.Add(next.model.ModelLabel + "  #" + next.model.DataFold + "   " + next.epochsToTrainTo.ToString(CultureInfo.InvariantCulture).PadLeft(6, '.') + "Begin");
                                        next.model.TrainUpto(next.epochsToTrainTo);
                                        toLog.Add(next.model.ModelLabel + "  #" + next.model.DataFold + "   " + next.epochsToTrainTo.ToString(CultureInfo.InvariantCulture).PadLeft(6, '.') + "End");
                                    }
                                },
                                cancel,
                                TaskCreationOptions.None,
                                LowPriorityTaskScheduler.DefaultLowPriorityScheduler
                            )
                    ).ToArray();

                Task.WaitAll(helpers, CancellationToken.None);
            } finally {
                _ = Interlocked.Decrement(ref trainersRunning);
            }
        }

        public void ResetLearningRate()
        {
            foreach (var model in subModels) {
                model.ResetLearningRate();
            }
        }

        public static string ItersPrefix(long iters)
        {
            var pow10 = (int)(Math.Log10(iters) + Math.Log10(10.0 / 9.5));
            var prefix = (int)(iters / Math.Pow(10.0, pow10) + 0.5);
            return (prefix == 1 ? "" : prefix.ToString()) + "e" + pow10;
        }

        public static readonly DirectoryInfo statsDir = FSUtil.FindDataDir(@"uni\Thesis\doc\stats", typeof(LvqStatPlotsContainer));

        static FileInfo StatFile(LvqDatasetCli dataset, LvqModelSettingsCli modelSettings, long iterIntent)
        {
            var dSettings = CreateDataset.CreateFactory(dataset.DatasetLabel);
            var dSettingsShorthand = dSettings.Shorthand;
            var datasetDir = statsDir.GetDirectories().FirstOrDefault(
                dir => {
                    var otherSettings = CreateDataset.CreateFactory(dir.Name);
                    return otherSettings != null && otherSettings.Shorthand == dSettingsShorthand;
                }
            ) ?? statsDir.CreateSubdirectory(dSettingsShorthand);
            var iterPrefix = ItersPrefix(iterIntent) + "-";
            var mSettingsShorthand = modelSettings.ToShorthand();

            return datasetDir.GetFiles(iterPrefix + "*.txt").FirstOrDefault(
                file => {
                    var otherSettings = CreateLvqModelValues.TryParseShorthand(Path.GetFileNameWithoutExtension(file.Name)[iterPrefix.Length..]);
                    return otherSettings.HasValue && otherSettings.Value.ToShorthand() == mSettingsShorthand;
                }
            ) ?? new FileInfo(Path.Combine(datasetDir.FullName + "\\", iterPrefix + mSettingsShorthand + ".txt"));
        }

        public static bool AnnounceModelTrainingGeneration(LvqDatasetCli dataset, LvqModelSettingsCli shorthand, long iterIntent)
        {
            var statFile = StatFile(dataset, shorthand, iterIntent);
            var isFresh = !statFile.Exists;
            if (isFresh) {
                File.WriteAllText(statFile.FullName, "");
            }

            return isFresh;
        }

        public void SaveStats(long iterIntent)
        {
            var allstats = EvaluateFullStats().ToArray();

            if (ItersPrefix(iterIntent) != ItersPrefix((long)Math.Round(allstats.Select(stat => stat.values[LvqTrainingStatCli.TrainingIterationI]).Average()))) {
                throw new InvalidOperationException("Trained the wrong number of iterations; aborting.");
            }

            var statsString = FullStatsString(allstats);

            var statFile = StatFile(InitSet, OriginalSettings, iterIntent);
            File.WriteAllText(statFile.FullName, statsString);
        }

        public MatrixContainer<byte> ClassBoundaries(
            int subModelIdx,
            double x0,
            double x1,
            double y0,
            double y1,
            int xCols,
            int yRows)
            => subModels[subModelIdx].ClassBoundaries(x0, x1, y0, y1, xCols, yRows);

        public ModelProjection CurrentModelProjection(int subModelIdx, bool showTestEmbedding)
            => subModels[subModelIdx].CurrentProjectionAndPrototypes(showTestEmbedding);

        public sealed class ModelProjectionAndImage
        {
            public LabelledPoint[] RawPoints;
            public Point[][] PrototypesByLabel;
            public Point[][] PointsByLabel;
            public Rect Bounds;
            public int Width, Height;
            public uint[] ImageData;
            public LvqMultiModel forModels;
            public int forSubModel;
            public LvqDatasetCli forDataset;
        }

        public ModelProjectionAndImage CurrentProjectionAndImage(LvqDatasetCli dataset, int width, int height, bool hideBoundaries, int currSubModel, bool showTestEmbedding)
        { //TODO:testembed
#if DEBUG
            var renderwidth = (width + 7) / 8;
            var renderheight = (height + 7) / 8;
#else
            var renderwidth = width;
            var renderheight = height;
#endif
            var selectedModel = subModels[currSubModel];
            ModelProjection projection;
            Rect bounds;
            MatrixContainer<byte> closestClass;
            lock (selectedModel.ReadSync) {
                projection = selectedModel.CurrentProjectionAndPrototypes(showTestEmbedding);
                if (!projection.HasValue) {
                    return null;
                }

                bounds = ExpandToShape(renderwidth, renderheight, ComputeProjectionBounds(projection.Points));
                closestClass = hideBoundaries
                    ? default(MatrixContainer<byte>)
                    : selectedModel.ClassBoundaries(bounds.Left, bounds.Right, bounds.Bottom, bounds.Top, renderwidth, renderheight);
            }

            Debug.Assert(NotDefault(projection));
            Debug.Assert(NotDefault(bounds));

            var nativeColorsPerClass = NativeColorsPerClassAndBlack(dataset);
            var boundaryImage = closestClass.IsSet() ? BoundaryImageFor(closestClass, nativeColorsPerClass, width, renderwidth, height, renderheight) : null;
            return new() {
                Width = width,
                Height = height,
                ImageData = boundaryImage,
                Bounds = bounds,
                PrototypesByLabel = GroupPointsByLabel(projection.Prototypes, dataset.ClassCount),
                PointsByLabel = GroupPointsByLabel(projection.Points, dataset.ClassCount),
                RawPoints = projection.Points.Select(lp => new LabelledPoint { label = lp.label, point = lp.point }).ToArray(),
                forDataset = dataset,
                forModels = this,
                forSubModel = currSubModel,
            };
        }

        static Point[] ToPointArray(CliLvqLabelledPoint[] cliLvqLabelledPoint)
        {
            var retval = new Point[cliLvqLabelledPoint.Length];
            for (var i = 0; i < retval.Length; i++) {
                retval[i] = cliLvqLabelledPoint[i].point;
            }

            return retval;
        }

        static Rect ExpandToShape(int width, int height, Rect rect)
        {
            width = Math.Max(width, 1);
            height = Math.Max(height, 1);
            if (rect.Width / width < rect.Height / height) {
                var scale = rect.Height / height / (rect.Width / width);
                return new(rect.X - rect.Width * (scale - 1) / 2, rect.Y, rect.Width * scale, rect.Height);
            } else {
                var scale = rect.Width / width / (rect.Height / height);
                return new(rect.X, rect.Y - rect.Height * (scale - 1) / 2, rect.Width, rect.Height * scale);
            }
        }

        static Point[][] GroupPointsByLabel(CliLvqLabelledPoint[] labelledPoints, int classCount)
        {
            //var projectedPointsByLabel = Enumerable.Range(0, dataPoints.Length).ToLookup(i => currProjection.Data.ClassLabels[i], i =>  Points.GetPoint(currProjection.Data.Points, i));
            var pointCountPerClass = new int[classCount];
            foreach (var p in labelledPoints) {
                pointCountPerClass[p.label]++;
            }

            var pointsByLabel = pointCountPerClass.Select(pointCount => new Point[pointCount]).ToArray();
            var pointIndexPerClass = new int[classCount];
            for (var i = 0; i < labelledPoints.Length; ++i) {
                var label = labelledPoints[i].label;
                pointsByLabel[label][pointIndexPerClass[label]++] = labelledPoints[i].point;
            }

            return pointsByLabel;
        }

        struct IntPoint
        {
            public int X, Y;
        }

        static uint[] BoundaryImageFor(MatrixContainer<byte> closestClass, uint[] nativeColorsPerClass, int width, int renderwidth, int height, int renderheight)
        {
            var boundaryPoints = GetBoundaryPoints(closestClass);
            MakeBoundaryBlack(closestClass, boundaryPoints, (byte)(nativeColorsPerClass.Length - 1));
            return ToNativeColorBmp(closestClass, nativeColorsPerClass, width, renderwidth, height, renderheight);
        }

        static uint[] ToNativeColorBmp(MatrixContainer<byte> closestClass, uint[] nativeColorsPerClass, int width, int renderwidth, int height, int renderheight)
        {
            var classboundaries = new uint[width * height];
            var px = 0;
            for (var y = 0; y < height; y++) {
                for (var x = 0; x < width; x++) {
                    classboundaries[px++] = nativeColorsPerClass[closestClass[y * renderheight / height, x * renderwidth / width]];
                }
            }

            return classboundaries;
        }

        static void MakeBoundaryBlack(MatrixContainer<byte> closestClass, List<IntPoint> boundaryPoints, byte blackIdx)
        {
            foreach (var coord in boundaryPoints) {
                closestClass.Set(coord.Y, coord.X, blackIdx);
            }
        }

        static List<IntPoint> GetBoundaryPoints(MatrixContainer<byte> closestClass)
        {
            var edges = new List<IntPoint>();
            for (var y = 1; y < closestClass.rows - 1; y++) {
                for (var x = 1; x < closestClass.cols - 1; x++) {
                    var addr = closestClass.cols * y + x;
                    var val = closestClass.arr[addr];
                    if (val != closestClass.arr[addr - 1]
                        || val != closestClass.arr[addr + 1]
                        || val != closestClass.arr[addr - closestClass.cols]
                        || val != closestClass.arr[addr + closestClass.cols]
                    ) {
                        edges.Add(new() { X = x, Y = y });
                    }
                }
            }

            return edges;
        }

        static bool NotDefault<T>(T val)
            => !Equals(val, default(T));

        static uint[] NativeColorsPerClassAndBlack(LvqDatasetCli dataset)
            => dataset.ClassColors
                .Select(
                    c => {
                        c.ScA = 0.05f;
                        return c;
                    }
                )
                .Concat(Enumerable.Repeat(Color.FromRgb(0, 0, 0), 1))
                .Select(c => c.ToNativeColor())
                .ToArray();

        static Rect ComputeProjectionBounds(CliLvqLabelledPoint[] cliLvqLabelledPoint)
        {
            VizPixelScatterHelpers.RecomputeBounds(ToPointArray(cliLvqLabelledPoint), 0.95, 0.95, 10.0, out _, out var inner);
            //            inner.Union(VizPixelScatterHelpers.ComputeOuterBounds(prototypePositions.ToArray()));
            return inner;
        }

        public IEnumerable<LvqModelCli> SubModels
            => subModels;

        public object Tag { get; set; }
    }
}
