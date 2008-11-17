using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions;
using SongDataLib;
using LastFMspider;
using System.IO;
using System.Text.RegularExpressions;
using hitmds;
using System.Threading;
using EmnExtensions.Algorithms;
using System.Windows;
//using EmnExtensions.Wpf;
using System.Windows.Media;
using LastFMspider.LastFMSQLiteBackend;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SimilarityMdsLib
{
    public class DistFormatLoader
    {

        const int MAX_MDS_ITEM_COUNT = 13000;

        public readonly IProgressManager progress;
        //public readonly MusicMdsDisplay mainDisplay;
        public readonly SimilarityFormat format;

        SimCacheManager _settings;
        CachedDistanceMatrix _cachedMatrix;
        TestDataInTraining _evaluator;
        public SimCacheManager Settings { get {
                lock (this) {
                    if (_settings == null) {
                        var config = new SongDatabaseConfigFile(false);
                        var tools = new LastFmTools(config);
                        _settings = new SimCacheManager(format, tools, DataSetType.Training);

                    }
                    return _settings;
                }
        } }
        readonly bool extendCache;
        public DistFormatLoader(IProgressManager progress, SimilarityFormat format,bool extendIfPossible) {
            this.progress = progress;
            this.format = format;
            extendCache = extendIfPossible;
        }

        public CachedDistanceMatrix CachedMatrix {
            get {
                lock (this) {
                    if (_cachedMatrix == null) {
                        progress.NewTask("Load Distance Matrix " + Settings.Format);
                        _cachedMatrix = Settings.LoadCachedDistanceMatrix();

                        if (extendCache) {
                            progress.SetProgress(0.5, "Extending Cache");
                            _cachedMatrix.LoadDistFromAllCacheFiles(d => { progress.SetProgress(d); }, true);
                        }

                        progress.SetProgress(0.6, "Trimming and counting Dataset");
                        while (_cachedMatrix.Mapping.Count > MAX_MDS_ITEM_COUNT)
                            _cachedMatrix.Mapping.ExtractAndRemoveLast();
                        _cachedMatrix.Matrix.ElementCount = _cachedMatrix.Mapping.Count;

                        //remove infinities:
                        var arr = _cachedMatrix.Matrix.DirectArrayAccess();
                        float max = 0.0f;
                        List<int> infIndexes = new List<int>();
                        for (int i = 0; i < _cachedMatrix.Matrix.DistCount; i++) {
                            if (!arr[i].IsFinite()) infIndexes.Add(i);
                            else if (arr[i] > max) max = arr[i];
                        }
                        foreach (int infIndex in infIndexes)
                            arr[infIndex] = max * 10;//anything, as long as it's FAR away but not infinite.
                        _maxDist = max;
                        Console.WriteLine("dists: {0} total, {1}% finite", _cachedMatrix.Matrix.DistCount, 100.0 * (1 - infIndexes.Count / (double)_cachedMatrix.Matrix.DistCount));
                        infIndexes = null;
                    }
                    return _cachedMatrix;
                }
            }
        }
        double _maxDist=double.NaN;
        public double MaxDistance {
            get {
                lock (this) {
                    if (!_maxDist.IsFinite()) {
                        var cachedMatrix = CachedMatrix;
                    }
                    return _maxDist;
                }
            }
        }

        public TestDataInTraining Evaluator {
            get {
                lock (this) {
                    if (_evaluator == null) {
                        progress.NewTask("Finding relevant matches in test-data");
                        _evaluator = new TestDataInTraining(Settings, CachedMatrix);
                        progress.Done();

                    }
                    return _evaluator;
                }
            }
        }


        public MdsEngine ConstructMdsEngine(MdsEngine.Options Opts) {
            return new MdsEngine(Settings, Evaluator, CachedMatrix, Opts);
        }

        public void Run(MdsEngine.Options Opts, bool rerunEvenIfCached) {

            MdsEngine engine = ConstructMdsEngine(Opts);
            NiceTimer timer = new NiceTimer();
            if (engine.ResultsAlreadyCached && !rerunEvenIfCached) {
                Console.WriteLine("Cached: " + engine.resultsFilename);

                return;
            } else {
                timer.TimeMark("Calc: " + engine.resultsFilename);
            }

            engine.DoMds();

                engine.SaveMds();
            timer.Done();
        }

        private void CalculateDistanceHistogram(CachedDistanceMatrix cachedMatrix) {
            progress.NewTask("Calculate Distance Histogram for " + cachedMatrix.Settings.Format);
            var histData = new Histogrammer(
                CachedMatrix.Matrix.Values.Select(f => (double)f), cachedMatrix.Mapping.Count, 2000
                ).GenerateHistogram().ToArray();

            /*mainDisplay.Dispatcher.BeginInvoke((Action)delegate {
                var graph = mainDisplay.HistogramControl.NewGraph("dist" + cachedMatrix.Settings.Format, new Point[] { });
                foreach (var p in histData.Select(data => new Point(data.point, data.density)))
                    graph.AddPoint(p);
                var bounds = graph.GraphBounds;
                graph.GraphBounds = new Rect(bounds.X, 0.0, bounds.Width, bounds.Height + bounds.Top);
                mainDisplay.HistogramControl.ShowGraph(graph);
                histData = null;
            });*/ //TODO move to UI
            progress.Done();
        }


        private void FindBillboardHits(double[,] positionedPoints) {
            progress.NewTask("Finding Billboard hits");

            Dictionary<int, SongRef> songrefByMds = BillboardByMdsId.TracksByMdsId(CachedMatrix);
            progress.SetProgress(0.5);
            FileInfo logFile = new FileInfo(Path.Combine(Settings.DataDirectory.FullName, @".\mdsPoints-" + Settings.Format + ".txt"));

            using (Stream s = logFile.Open(FileMode.Create, FileAccess.Write))
            using (TextWriter writer = new StreamWriter(s)) {
                for (int i = 0; i < positionedPoints.GetLength(0); i++) {
                    if (songrefByMds.ContainsKey(i)) {
                        writer.WriteLine("{0}:    {1}",
                            string.Join(", ", Enumerable.Range(0, positionedPoints.GetLength(1))
                            .Select(j => string.Format("{0,10:G6}", positionedPoints[i, j]))
                            .ToArray())
                            , songrefByMds[i]);
                    }
                    progress.SetProgress(0.5 + 0.5 * (i + 1.0) / positionedPoints.GetLength(0));
                }
            }
            progress.Done();
        }


    }
}
