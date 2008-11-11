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

        bool doneInit = false;
        SimCacheManager settings;
        CachedDistanceMatrix cachedMatrix;
        TestDataInTraining evaluator;

        public DistFormatLoader(IProgressManager progress, SimilarityFormat format) {
            this.progress = progress;
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

            //remove infinities:
            var arr=cachedMatrix.Matrix.DirectArrayAccess();
            float max=0.0f;
            List<int> infIndexes = new List<int>();
            for (int i = 0; i < cachedMatrix.Matrix.DistCount; i++) {
                if (!arr[i].IsFinite()) infIndexes.Add(i);
                else if (arr[i] > max) max = arr[i];
            }
            foreach (int infIndex in infIndexes)
                arr[infIndex] = max*10;//anything, as long as it's FAR away but not infinite.
            Console.WriteLine("dists: {0} total, {1}% finite", cachedMatrix.Matrix.DistCount, 100.0 * (1 - infIndexes.Count / (double)cachedMatrix.Matrix.DistCount));
            infIndexes = null;


            progress.NewTask("Finding relevant matches in test-data");
            evaluator = new TestDataInTraining(settings, cachedMatrix);
            progress.Done();
            doneInit = true;
        }
        public void Run(MdsEngine.Options Opts, bool rerunEvenIfCached) {

            //  CalculateDistanceHistogram(cachedMatrix);
            


            //progress.NewTask("MDS");
            MdsEngine engine = new MdsEngine(settings, evaluator, cachedMatrix, Opts);
            NiceTimer timer = new NiceTimer();
            if (engine.ResultsAlreadyCached && !rerunEvenIfCached) {
                Console.WriteLine("Cached: " + engine.resultsFilename);

                return;
            } else {
                timer.TimeMark("Calc: " + engine.resultsFilename);
            }
            //TODO move to UI:
            //engine.Correlations.CollectionChanged += new NotifyCollectionChangedEventHandler(Correlations_CollectionChanged);
            //engine.TestSetRankings.CollectionChanged += new NotifyCollectionChangedEventHandler(TestSetRankings_CollectionChanged);

            engine.DoMds();
            engine.SaveMds();
            timer.Done();
            //progress.Done();
            //   FindBillboardHits(positionedPoints,settings,cachedMatrix);
        }
        /*
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
         * */
        //TODO move to UI

        private void CalculateDistanceHistogram(CachedDistanceMatrix cachedMatrix) {
            progress.NewTask("Calculate Distance Histogram for " + cachedMatrix.Settings.Format);
            var histData = new Histogrammer(
                cachedMatrix.Matrix.Values.Select(f => (double)f), cachedMatrix.Mapping.Count, 2000
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
