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
using EmnExtensions.Wpf;
using System.Windows.Media;
using LastFMspider.LastFMSQLiteBackend;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace RealSimilarityMds
{
    class Program
    {

        const int MAX_MDS_ITEM_COUNT = 13000;

        public readonly IProgressManager progress;
        public readonly MusicMdsDisplay mainDisplay;
        public readonly SimilarityFormat format;

        bool doneInit = false;
        SimCacheManager settings;
        CachedDistanceMatrix cachedMatrix;
        TestDataInTraining evaluator;

        public Program(IProgressManager progress, MusicMdsDisplay mainDisplay, SimilarityFormat format) {
            this.progress = progress;
            this.mainDisplay = mainDisplay;
            this.format = format;
        }

        public void Init() {
            if (doneInit) return;

            //Load Settings:
            var config = new SongDatabaseConfigFile(false);
            var tools = new LastFmTools(config);
            settings = new SimCacheManager(format, tools, DataSetType.Training);

            progress.NewTask("Load Distance Matrix " + settings.Format);
            cachedMatrix = settings.LoadCachedDistanceMatrix();

            progress.SetProgress(0.5, "Extending Cache");
            cachedMatrix.LoadDistFromAllCacheFiles(d => { progress.SetProgress(d); }, true);

            progress.SetProgress(0.6, "Trimming and counting Dataset");
            while (cachedMatrix.Mapping.Count > MAX_MDS_ITEM_COUNT)
                cachedMatrix.Mapping.ExtractAndRemoveLast();
            cachedMatrix.Matrix.ElementCount = cachedMatrix.Mapping.Count;
            int distCount = cachedMatrix.Matrix.Values.Count();
            int distFiniteCount = cachedMatrix.Matrix.Values.Where(f => f.IsFinite()).Count();
            Console.WriteLine("dists: {0} total, {1}% finite", distCount, 100.0 * distFiniteCount / (double)distCount);

            progress.NewTask("Finding relevant matches in test-data");
            evaluator = new TestDataInTraining(settings, cachedMatrix);
            progress.Done();
            doneInit = true;
        }
        public void Run(MdsEngine.Options Opts) {

            //  CalculateDistanceHistogram(cachedMatrix);
            


            //progress.NewTask("MDS");
            MdsEngine engine = new MdsEngine(settings, evaluator, cachedMatrix, Opts);
            //engine.Correlations.CollectionChanged += new NotifyCollectionChangedEventHandler(Correlations_CollectionChanged);
            //engine.TestSetRankings.CollectionChanged += new NotifyCollectionChangedEventHandler(TestSetRankings_CollectionChanged);
            engine.DoMds();
            engine.SaveMds();
            //progress.Done();
            //   FindBillboardHits(positionedPoints,settings,cachedMatrix);
        }

        void TestSetRankings_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (e.Action != NotifyCollectionChangedAction.Add)
                throw new Exception("WHooops!");
            mainDisplay.Dispatcher.BeginInvoke((Action)delegate {
                foreach (Point p in e.NewItems) {
                    mainDisplay.testSetRanking.AddPoint(p);
                }
            });
        }

        void Correlations_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (e.Action != NotifyCollectionChangedAction.Add)
                throw new Exception("WHooops!");
            mainDisplay.Dispatcher.BeginInvoke((Action)delegate {
                foreach (Point p in e.NewItems) {
                    mainDisplay.corrgraph.AddPoint(p);
                }
            });
        }

        private void CalculateDistanceHistogram(CachedDistanceMatrix cachedMatrix) {
            progress.NewTask("Calculate Distance Histogram for " + cachedMatrix.Settings.Format);
            var histData = new Histogrammer(
                cachedMatrix.Matrix.Values.Select(f => (double)f), cachedMatrix.Mapping.Count, 2000
                ).GenerateHistogram().ToArray();

            mainDisplay.Dispatcher.BeginInvoke((Action)delegate {
                var graph = mainDisplay.HistogramControl.NewGraph("dist" + cachedMatrix.Settings.Format, new Point[] { });
                foreach (var p in histData.Select(data => new Point(data.point, data.density)))
                    graph.AddPoint(p);
                var bounds = graph.GraphBounds;
                graph.GraphBounds = new Rect(bounds.X, 0.0, bounds.Width, bounds.Height + bounds.Top);
                mainDisplay.HistogramControl.ShowGraph(graph);
                histData = null;
            });
            progress.Done();
        }


        private void FindBillboardHits(double[,] positionedPoints) {
            progress.NewTask("Finding Billboard hits");

            Dictionary<int, SongRef> songrefByMds = BillboardByMdsId.TracksByMdsId(cachedMatrix);
            progress.SetProgress(0.5);
            FileInfo logFile = new FileInfo(Path.Combine(settings.DataDirectory.FullName, @".\mdsPoints-" + settings.Format + ".txt"));

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
