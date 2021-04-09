using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmnExtensions;
using EmnExtensions.Text;
using ExpressionToCodeLib;
using LvqLibCli;

namespace LvqGui
{
    public class LrOptimizer
    {
        public readonly uint offset;
        readonly LvqDatasetCli _dataset;
        readonly long _itersToRun;
        readonly int _folds;
        readonly DirectoryInfo datasetResultsDir;

        public LrOptimizer(long itersToRun, uint p_offset)
        {
            _itersToRun = itersToRun;
            offset = p_offset;
            _folds = basedatasets.Length;
            datasetResultsDir = ResultsDatasetDir(null);
        }

        public LrOptimizer(LvqDatasetCli dataset)
        {
            _itersToRun = 30L * 1000L * 1000L;
            offset = 0;
            _dataset = dataset;
            _folds = 3;
            datasetResultsDir = ResultsDatasetDir(dataset);
        }

        public Task TestLrIfNecessary(TextWriter sink, LvqModelSettingsCli settings, CancellationToken cancel)
        {
            if (!AttemptToClaimSettings(settings, sink)) {
                var sw = new StringWriter();
                return
                    Run(settings, sw, sink, cancel)
                        .ContinueWith(
                            t => {
                                using (sw) {
                                    if (t.Status == TaskStatus.RanToCompletion) {
                                        SaveLogFor(settings, sw.ToString());
                                    } else {
                                        AttemptToUnclaimSettings(settings, sink);
                                    }
                                }
                            },
                            cancel,
                            TaskContinuationOptions.ExecuteSynchronously,
                            LowPriorityTaskScheduler.DefaultLowPriorityScheduler
                        );
            }

            var tc = new TaskCompletionSource<int>();
            tc.SetResult(0);
            return tc.Task;
        }

        public static string ShortnameFor(long iters, LvqModelSettingsCli settings)
            => LvqMultiModel.ItersPrefix(iters) + "-" + settings.ToShorthand();

        public string ShortnameFor(LvqModelSettingsCli settings)
            => ShortnameFor(_itersToRun, settings);

        //{-8.46,0.42,-1.11,-1.49,1.51,0.79}
        static readonly double[,,] heurEffects = //proto,type,heur
        {
            {
                { 0, 0, 3.61, 2.40, 0, 0.48, 2.16, },
                { 0, 0, -8.53, -6.60, -0.27, -5.73, -0.28, },
                { 0, 0, 0.30, -5.34, 0, -2.48, 0, },
            }, {
                { -15.93, -0.54, 0.97, -5.54, 0, -0.48, -3.23, },
                { -9.72, -3.15, -2.71, 4.82, -1.06, 2.85, 1.47, },
                { 1.73, -0.79, -1.73, -6.40, 0, 2.77, 0, },
            },
        };

        static double EstimateAccuracy(LvqModelSettingsCli settings)
        {
            var protoIdx = settings.PrototypesPerClass == 1 ? 0 : 1;
            var typeIdx = settings.ModelType == LvqModelType.Ggm
                ? 0
                : settings.ModelType == LvqModelType.Gm
                    ? 2
                    : 1;
            var heurs = new[] { settings.NGi, settings.NGu, settings.Ppca, settings.SlowK, settings.Popt, settings.Bcov };
            return heurs.Select((on, heurIdx) => on ? heurEffects[protoIdx, typeIdx, heurIdx] : 0).Sum();
        }

        public enum LrTestingStatus { SomeUnstartedResults, SomeUnfinishedResults, AllResultsComplete }

        public static LrTestingStatus HasAllLrTestingResults(LvqDatasetCli dataset)
        {
            var testlr = new LrOptimizer(dataset);

            return (LrTestingStatus)
                testlr.AllLrTestingSettings().Min(
                    settings => {
                        var resultsFilePath = testlr.GetLogfilepath(settings).First();
                        return (int)(!resultsFilePath.Exists
                            ? LrTestingStatus.SomeUnstartedResults
                            : resultsFilePath.Length == 0
                                ? LrTestingStatus.SomeUnfinishedResults
                                : LrTestingStatus.AllResultsComplete);
                    }
                );
        }

        public Task StartAllLrTesting(CancellationToken cancel)
        {
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

        static readonly LvqModelSettingsCli[] AllLrTestingSettingsNoOffset =
        (
            from protoCount in new[] { 5, 1 }
            from modeltype in ModelTypes
            from ppca in new[] { true, false }
            from ngi in new[] { true, false }
            from slowbad in new[] { true, false }
            from NoB in new[] { true, false }
            from bi in new[] { true, false }
            from pi in new[] { true, false }
            from ng in new[] { true, false }
            let relevanceCost = new[] { ppca, ngi, slowbad, bi, pi, ng, NoB }.Count(b => b)
            //where relevanceCost ==0 || relevanceCost==1 && (ngi||slowbad||!rp)
            where relevanceCost <= 1
                || relevanceCost <= 2 && !NoB && !ng && !pi
                || !bi && !pi && !ng && !NoB
            //where relevanceCost <=1
            let settings = new LvqModelSettingsCli {
                ModelType = modeltype,
                PrototypesPerClass = protoCount,
                Ppca = ppca,
                NGi = ngi,
                NGu = ng,
                Bcov = bi,
                Popt = pi,
                SlowK = slowbad,
                wGMu = NoB,
            }
            where settings == settings.Canonicalize()
            let estAccur = EstimateAccuracy(settings)
            orderby relevanceCost, estAccur
            select settings).ToArray();

        IEnumerable<LvqModelSettingsCli> AllLrTestingSettings()
            =>
                from settings in AllLrTestingSettingsNoOffset
                select WithSeedFromOffset(settings, offset);

        Task FindOptimalLr(TextWriter sink, LvqModelSettingsCli settings, CancellationToken cancel)
        {
            var lr0range = _dataset != null ? LogRange(0.3 / settings.PrototypesPerClass, 0.01 / settings.PrototypesPerClass, 6) : LogRange(0.3, 0.01, 8);
            var lrPrange = _dataset != null ? LogRange(0.3 / settings.PrototypesPerClass, 0.01 / settings.PrototypesPerClass, 6) : LogRange(0.5, 0.03, 8);
            var lrBrange = settings.ModelType != LvqModelType.Ggm && settings.ModelType != LvqModelType.G2m && settings.ModelType != LvqModelType.Gpq && settings.ModelType != LvqModelType.Fgm
                    ? new[] { 0.0 }
                    : _dataset == null
                        ? LogRange(0.1, 0.003, 4)
                        : settings.ModelType == LvqModelType.G2m
                            ? LogRange(0.03 / settings.PrototypesPerClass, 0.001 / settings.PrototypesPerClass, 5)
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
                select ErrorOf(sink, _itersToRun, settings.WithLr(lr0, lrP, lrB), cancel)
            ).ToArray();

            return Task.Factory.ContinueWhenAll(
                errorRates,
                tasks => {
                    var lrAndErrors = tasks.Select(t => t.Result).ToArray();
                    foreach (var lrErr in lrAndErrors.OrderBy(lrErr => lrErr.Errors.CanonicalError)) {
                        sink.Write("\n" + lrErr.ToStorageString());
                    }

                    sink.WriteLine();
                },
                cancel,
                TaskContinuationOptions.ExecuteSynchronously,
                LowPriorityTaskScheduler.DefaultLowPriorityScheduler
            );
        }

        static IEnumerable<double> LogRange(double start, double end, int steps)
        {
            //start*exp(ln(end/start) *  i/(steps-1) )
            var lnScale = Math.Log(end / start);
            for (var i = 0; i < steps; i++) {
                yield return start * Math.Exp(lnScale * ((double)i / (steps - 1)));
            }
        }

        Task<LrAndError> ErrorOf(TextWriter sink, long iters, LvqModelSettingsCli settings, CancellationToken cancel)
        {
            var nnErrorIdx = -1;
            var results = new Task<LvqTrainingStatCli>[_folds];

            for (var i = 0; i < _folds; i++) {
                var dataset = _dataset ?? basedatasets[i];
                var fold = _dataset != null ? i : 0;
                results[i] = Task.Factory.StartNew(
                    () => {
                        var model = new LvqModelCli("model", dataset, fold, settings, false);

                        nnErrorIdx = model.TrainingStatNames.AsEnumerable().IndexOf(name => name.Contains("NN Error")); // threading irrelevant; all the same & atomic.
                        model.Train((int)(iters / dataset.PointCount(fold)), false, false);
                        return model.EvaluateStats();
                    },
                    cancel,
                    TaskCreationOptions.PreferFairness,
                    LowPriorityTaskScheduler.DefaultLowPriorityScheduler
                );
            }

            return Task.Factory.ContinueWhenAll(
                results,
                tasks => {
                    sink.Write(".");
                    return new LrAndError(settings, LvqMultiModel.MeanStdErrStats(tasks.Select(task => task.Result).ToArray()), nnErrorIdx);
                },
                cancel,
                TaskContinuationOptions.ExecuteSynchronously,
                LowPriorityTaskScheduler.DefaultLowPriorityScheduler
            );
        }

        static readonly object fsSync = new object();

        bool AttemptToClaimSettings(LvqModelSettingsCli settings, TextWriter sink)
        {
            var saveFile = GetLogfilepath(settings).First();
            lock (fsSync) {
                if (saveFile.Exists) {
                    if (saveFile.Length > 0) {
                        //Console.WriteLine("already done:" + DatasetLabel + "\\" + ShortnameFor(settings));
                        if (sink != null) {
                            sink.WriteLine("already done!");
                        }
                    } else {
                        //Console.WriteLine("already started:" + DatasetLabel + "\\" + ShortnameFor(settings));
                        if (sink != null) {
                            sink.WriteLine("already started!");
                        }
                    }

                    return true;
                }

                saveFile.Directory.Create();
                saveFile.Create().Close(); //this is in general handy, but I'll manage it manually for now rather than leave around 0-byte files from failed runs.
                Console.WriteLine("Now starting:" + DatasetLabel + "\\" + ShortnameFor(settings) + " (" + DateTime.Now + ")");
                return false;
            }
        }

        void AttemptToUnclaimSettings(LvqModelSettingsCli settings, TextWriter sink)
        {
            var saveFile = GetLogfilepath(settings).First();
            lock (fsSync) {
                if (!saveFile.Exists) {
                    var message = "Already cancelled??? " + DatasetLabel + "\\" + ShortnameFor(settings);
                    Console.WriteLine(message);
                    if (sink != null) {
                        sink.WriteLine(message);
                    }
                } else if (saveFile.Length > 0) {
                    var message = "Won't cancel completed results:" + DatasetLabel + "\\" + ShortnameFor(settings);
                    Console.WriteLine(message);
                    if (sink != null) {
                        sink.WriteLine(message);
                    }
                } else {
                    var message = "Cancelling:" + DatasetLabel + "\\" + ShortnameFor(settings);
                    Console.WriteLine(message);
                    if (sink != null) {
                        sink.WriteLine(message);
                    }

                    saveFile.Delete();
                }
            }
        }

        Task Run(LvqModelSettingsCli settings, StringWriter sw, TextWriter extraSink, CancellationToken cancel)
        {
            if (extraSink == null) {
                return Run(settings, TextWriter.Synchronized(sw), cancel);
            }

            var effWriter = new ForkingTextWriter(new[] { sw, extraSink }, false);
            return Run(settings, TextWriter.Synchronized(effWriter), cancel)
                .ContinueWith(
                    _ => effWriter.Dispose(),
                    TaskContinuationOptions.ExecuteSynchronously
                );
        }

        Task Run(LvqModelSettingsCli settings, TextWriter sink, CancellationToken cancel)
        {
            sink.WriteLine("Evaluating: " + settings.ToShorthand());
            sink.WriteLine("Against: " + DatasetLabel);
            var timer = Stopwatch.StartNew();
            LowPriorityTaskScheduler.DefaultLowPriorityScheduler.StartNewTask(() => timer = Stopwatch.StartNew());

            return FindOptimalLr(sink, settings, cancel).ContinueWith(
                _ => {
                    var durationSec = timer.Elapsed.TotalSeconds;
                    sink.WriteLine("Search Complete!  Took " + durationSec + "s");
                    Console.WriteLine("Optimizing " + ShortnameFor(settings) + " took " + durationSec + "s (" + DateTime.Now + ")");
                },
                TaskContinuationOptions.ExecuteSynchronously
            );
        }

        string DatasetLabel
            => _dataset != null ? _dataset.DatasetLabel : "base";

        void SaveLogFor(LvqModelSettingsCli settings, string logcontents)
        {
            var logfilepath = GetLogfilepath(settings).First(path => path.Exists || path.Length == 0);
            logfilepath.Directory.Create();
            File.WriteAllText(logfilepath.FullName, logcontents);
        }

        IEnumerable<FileInfo> GetLogfilepath(LvqModelSettingsCli settings)
            => new[] { SettingsFile(settings) }.Concat(
                Enumerable.Range(1, 1000)
                    .Select(i => ShortnameFor(settings) + " (" + i + ")" + ".txt")
                    .Select(filename => new FileInfo(Path.Combine(LrGuesser.resultsDir.FullName, DatasetLabel + "\\" + filename)))
            );

        FileInfo SettingsFile(LvqModelSettingsCli settings)
        {
            var mSettingsShorthand = settings.ToShorthand();
            var prefix = LvqMultiModel.ItersPrefix(_itersToRun) + "-";

            return new FileInfo(Path.Combine(datasetResultsDir.FullName + "\\", prefix + mSettingsShorthand + ".txt"));
        }

        static DirectoryInfo ResultsDatasetDir(LvqDatasetCli dataset)
        {
            if (dataset == null) {
                return LrGuesser.resultsDir.CreateSubdirectory("base");
            }

            return new DirectoryInfo(LrGuesser.resultsDir.FullName + "\\" + dataset.DatasetLabel);
        }

        public static IEnumerable<LvqModelType> ModelTypes
            => new[] { LvqModelType.Ggm, LvqModelType.G2m, LvqModelType.Gm }; //  (LvqModelType[])Enum.GetValues(typeof(LvqModelType)); } }

        public static IEnumerable<int> PrototypesPerClassOpts
        {
            get {
                yield return 5;
                yield return 1;
            }
        }

        static IEnumerable<LvqDatasetCli> Datasets()
        {
            // ReSharper disable RedundantAssignment
            uint rngParam = 1000;
            uint rngInst = 1001;
            yield return new GaussianCloudDatasetSettings { ParamsSeed = rngParam++, InstanceSeed = rngInst++, Dimensions = 16, PointsPerClass = (int)(10000 / Math.Sqrt(16) / 3), }.CreateDataset();
            yield return new GaussianCloudDatasetSettings { ParamsSeed = rngParam++, InstanceSeed = rngInst++, Dimensions = 8, PointsPerClass = (int)(10000 / Math.Sqrt(8) / 3), }.CreateDataset();
            yield return new StarDatasetSettings { ParamsSeed = rngParam++, InstanceSeed = rngInst++, Dimensions = 12, ClusterDimensionality = 6, NumberOfClusters = 3, NumberOfClasses = 4, PointsPerClass = (int)(10000 / Math.Sqrt(12) / 4), NoiseSigma = 2.5, }.CreateDataset();
            yield return new StarDatasetSettings { ParamsSeed = rngParam++, InstanceSeed = rngInst++, Dimensions = 8, ClusterDimensionality = 4, NumberOfClusters = 3, PointsPerClass = (int)(10000 / Math.Sqrt(8) / 3), NoiseSigma = 2.5, }.CreateDataset();
            yield return LoadDatasetImpl.Load(10, "segmentationNormed_combined.data", rngInst++);
            yield return LoadDatasetImpl.Load(10, "colorado.data", rngInst++);
            yield return LoadDatasetImpl.Load(10, "pendigits.train.data", rngInst++);
            // ReSharper restore RedundantAssignment
        }

        static LvqDatasetCli[] m_basedatasets;

        static LvqDatasetCli[] basedatasets
            => m_basedatasets ?? (m_basedatasets = Datasets().ToArray());

        public static LvqModelSettingsCli WithSeedFromOffset(LvqModelSettingsCli settings, uint offset)
            => settings.WithSeeds(1 + 2 * offset, 2 * offset);
    }
}
