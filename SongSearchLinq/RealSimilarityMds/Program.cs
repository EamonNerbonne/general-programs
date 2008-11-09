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

namespace RealSimilarityMds
{
    class Program
    {
        static Random GraphColorRandom = new Random();
        static Brush RandomGraphColor() {
            double r, g, b, sum;
            r = GraphColorRandom.NextDouble() + 0.01;
            g = GraphColorRandom.NextDouble() + 0.01;
            b = GraphColorRandom.NextDouble() + 0.01;
            sum = GraphColorRandom.NextDouble() * 0.5 + 0.5;
            SolidColorBrush brush = new SolidColorBrush(
                new Color {
                    A = (byte)255,
                    R = (byte)(255 * r * sum / (r + g + b)),
                    G = (byte)(255 * g * sum / (r + g + b)),
                    B = (byte)(255 * b * sum / (r + g + b)),
                }
                );
            brush.Freeze();
            return brush;
        }



        const int GENERATIONS = 20;
        const double LEARN_RATE = 2.0;
        const double START_ANNEALING = 0.0;
        const int maxMdsCount = 13000;
        const int POINT_UPDATE_STYLE = 2;
        const int DIMENSIONS = 20;
        const SimilarityFormat SIMFORMAT = SimilarityFormat.Log2000;

        static Regex fileNameRegex = new Regex(@"(e|t|b)(?<num>\d+)\.(dist|bin)", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        ProgressManager progress;
        MusicMdsDisplay mainDisplay;
        SimilarityFormat format;
        Thread background;
        public Program(ProgressManager progress, MusicMdsDisplay mainDisplay, SimilarityFormat format) {
            this.progress = progress;
            this.mainDisplay = mainDisplay;
            this.format = SIMFORMAT;
            background = new Thread(Run) {
                IsBackground = true,
                Priority = ThreadPriority.Normal
            };
        }
        public void RunInBackground() {
            background.Start();
        }

        void Run() {
            var config = new SongDatabaseConfigFile(false);
            var tools = new LastFmTools(config);
            var settings = new SimCacheManager(format, tools, DataSetType.Training);
            Run2(settings);
        }

        void Run2(SimCacheManager settings) {
            NiceTimer timer = new NiceTimer();
            timer.TimeMark("Load Distance Matrix " + settings.Format);
            CachedDistanceMatrix cachedMatrix = settings.LoadCachedDistanceMatrix();
            progress.NewTask("Configuring", 1.0);
            cachedMatrix.LoadDistFromAllCacheFiles(d => { progress.SetProgress(d); }, true);
            timer.TimeMark("trimming");
            int distCount = cachedMatrix.Matrix.Values.Count();
            int distFiniteCount = cachedMatrix.Matrix.Values.Where(f => f.IsFinite()).Count();
            while (cachedMatrix.Mapping.Count > maxMdsCount)
                cachedMatrix.Mapping.ExtractAndRemoveLast();
            Console.WriteLine("dists: {0} total, {1}% finite", distCount, 100.0 * distFiniteCount / (double)distCount);
            timer.TimeMark("Calculate Distance Histogram for " + settings.Format);
#if CALC_DIST_HISTO
            var histData = new Histogrammer(
                cachedMatrix.Matrix.Values.Select(f => (double)f), cachedMatrix.Mapping.Count, 2000
                ).GenerateHistogram().ToArray();

            mainDisplay.Dispatcher.BeginInvoke((Action)delegate {
                var graph = mainDisplay.HistogramControl.NewGraph("dist" + settings.Format, new Point[] { });
                foreach (var p in histData.Select(data => new Point(data.point, data.density)))
                    graph.AddPoint(p);
                var bounds = graph.GraphBounds;
                graph.GraphLineColor = RandomGraphColor();
                graph.GraphBounds = new Rect(bounds.X, 0.0, bounds.Width, bounds.Height + bounds.Top);
                mainDisplay.HistogramControl.ShowGraph(graph);
                histData = null;
            });
#endif

            timer.TimeMark("Finding relevant matches in test-data");

            evaluator= new TestDataInTraining(settings, cachedMatrix);

            System.GC.Collect();
            timer.TimeMark("MDS");
            double[,] positionedPoints = DoMds(cachedMatrix);
            timer.TimeMark("saving");
            FileInfo mdsFile = new FileInfo(Path.Combine(settings.DataDirectory.FullName, @".\mdsPoints-" + settings.Format + ".bin"));
            using (Stream s = mdsFile.Open(FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(s)) {
                writer.Write((int)positionedPoints.GetLength(0));//songCount
                writer.Write((int)positionedPoints.GetLength(1));//dimCount

                for (int i = 0; i < positionedPoints.GetLength(0); i++) { //mdsSongIndex
                    for (int dim = 0; dim < positionedPoints.GetLength(1); dim++) {
                        writer.Write((double)positionedPoints[i, dim]);
                    }
                }
            }


            timer.TimeMark("Finding Billboard hits");
            progress.NewTask("Finding Billboard hits", 1.0);

            Dictionary<int, SongRef> songrefByMds = BillboardByMdsId.TracksByMdsId(cachedMatrix);

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
                }
            }

            timer.Done();
            progress.NewTask("Done!", 1.0);
        }



        TestDataInTraining evaluator;
        int nextGraphUpdateOn = 1;
        int testCheckIter = 0;
        const int graphRes = 1000;
        const int testCheckMult = 10;
        GraphControl corrgraph;
        GraphControl testSetRanking;
        void DoProgress(int cycle, int ofTotal, Hitmds src) {
            double progressRatio = cycle / (double)ofTotal;
            if ((int)(progressRatio * graphRes) >= nextGraphUpdateOn) {
                AddPoint(corrgraph, new Point(progressRatio, CalcCorr(src)));
                nextGraphUpdateOn++;
                if (testCheckIter == 0) {
                    AddPoint(testSetRanking,new Point(progressRatio,CalcRanking(src)));
                }
                testCheckIter = (testCheckIter + 1) % testCheckMult;
            }
            progress.SetProgress(progressRatio);
        }

        static double CalcCorr(Hitmds mdsImpl) {
            return 1 / Math.Sqrt(mdsImpl.corr_2() + 1);
        }

        double CalcRanking(Hitmds mdsImpl) {
            return 1 - evaluator.AverageRanking(mdsImpl.DistsTo);
        }

        static void AddPoint(GraphControl toControl, Point p) {
            toControl.Dispatcher.BeginInvoke((Action<Point>)toControl.AddPoint, p);
        }



        private static double[,] ExtractDims(Hitmds mdsImpl, int dimensions, int mdsIdCount) {
            double[,] retval = new double[mdsIdCount, dimensions];
            for (int mdsId = 0; mdsId < mdsIdCount; mdsId++)
                for (int dim = 0; dim < dimensions; dim++)
                    retval[mdsId, dim] = mdsImpl.GetPoint(mdsId, dim);
            return retval;
        }

        private double[,] DoMds(CachedDistanceMatrix cachedMatrix) {
            Random r = new Random();

            int totalRounds = cachedMatrix.Mapping.Count * GENERATIONS;

            progress.NewTask("MDS-init", 1.0);

            float maxDist = cachedMatrix.Matrix.Values.Where(dist => dist.IsFinite()).Max();
            mainDisplay.Dispatcher.Invoke((Action)delegate {
                corrgraph = mainDisplay.HistogramControl.NewGraph("corr" + cachedMatrix.Settings.Format, new Point[] { });
                corrgraph.GraphLineColor = RandomGraphColor();
                corrgraph.EnsurePointInBounds(new Point(0, 0));
                corrgraph.EnsurePointInBounds(new Point(1, 1));
                mainDisplay.HistogramControl.ShowGraph(corrgraph);

                testSetRanking = mainDisplay.HistogramControl.NewGraph("testRank" + cachedMatrix.Settings.Format, new Point[] { });
                testSetRanking.GraphLineColor = RandomGraphColor();
                testSetRanking.EnsurePointInBounds(new Point(0, 0.5));
                testSetRanking.EnsurePointInBounds(new Point(1, 1-200.0/cachedMatrix.Mapping.Count));
                mainDisplay.HistogramControl.ShowGraph(testSetRanking);


            });

            progress.NewTask("MDS", 1.0);
            using (Hitmds mdsImpl = new Hitmds(cachedMatrix.Mapping.Count, DIMENSIONS, (i, j) => cachedMatrix.Matrix[i, j].IsFinite() ? cachedMatrix.Matrix[i, j] : maxDist * 10, r)) {
                nextGraphUpdateOn = 1;
                mdsImpl.mds_train(totalRounds, LEARN_RATE, START_ANNEALING, DoProgress, POINT_UPDATE_STYLE);
                AddPoint(corrgraph, new Point(1.0, CalcCorr(mdsImpl)));
                AddPoint(testSetRanking, new Point(1.0,CalcRanking(mdsImpl)));

                double[,] retval = mdsImpl.PointPositions();
                progress.SetProgress(1.0);
                return retval;
            }
        }
    }
}
