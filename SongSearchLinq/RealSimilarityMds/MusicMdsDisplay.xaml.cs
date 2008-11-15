using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using EmnExtensions.Wpf;
using LastFMspider;
using EmnExtensions;
using EmnExtensions.Algorithms;
using SimilarityMdsLib;
using SongDataLib;
using System.IO;
using System.Collections.Specialized;
using Microsoft.Win32;
using EmnExtensions.Threading;

namespace RealSimilarityMds
{
    public partial class MusicMdsDisplay : Window
    {
        ProgressManager progress;
        //Program program;
        public PlotControl PlotControl { get { return this.distanceHistoview; } }

        SimCacheManager settings;
        public MusicMdsDisplay() {
            settings = new SimCacheManager(SimilarityFormat.LastFmRating, new LastFmTools(new SongDatabaseConfigFile(false)), DataSetType.Training);
            InitializeComponent();
            comboBoxFMT.ItemsSource = fmtValues;
            comboBoxLR.ItemsSource = lrValues;
            comboBoxSA.ItemsSource = saValues;
            comboBoxPUS.ItemsSource = pusValues;
            comboBoxGEN.ItemsSource = genValues;
            comboBoxDIM.ItemsSource = dimValues;

            comboBoxFMT.SelectedItem = SimilarityFormat.AvgRank2;
            comboBoxLR.SelectedItem = 1.0;
            comboBoxSA.SelectedItem = 0.0;
            comboBoxPUS.SelectedItem = PointUpdate.ConstantRandom;
            comboBoxGEN.SelectedItem = 30;
            comboBoxDIM.SelectedItem = 20;

            progress = new ProgressManager(this.progressBar, this.labelETA, new NiceTimer());

            graphPrintBox.ItemsSource = PlotControl.Graphs;
        }

        SimilarityFormat[] fmtValues = new[] { SimilarityFormat.AvgRank, SimilarityFormat.AvgRank2, SimilarityFormat.Log200, SimilarityFormat.Log2000 };
        double[] lrValues = new[] { 1.0, 5.0, 2.0 };
        double[] saValues = new[] { 0.0, 0.5 };
        enum PointUpdate { ConstantRandom = 1, DecreasingRandom = 2, OriginalHiTmds = 0, }
        PointUpdate[] pusValues = new[] { PointUpdate.ConstantRandom, PointUpdate.DecreasingRandom, PointUpdate.OriginalHiTmds };
        int[] genValues = new[] { 20, 30, 50 };
        int[] dimValues = new[] { 2, 3, 5, 10, 20, 50 };

        MdsEngine.Options SelectedOptions {
            get {
                var lr = double.Parse(comboBoxLR.Text);
                var sa = double.Parse(comboBoxSA.Text);
                var pus = (PointUpdate)comboBoxPUS.SelectedItem;
                var gen = int.Parse(comboBoxGEN.Text);
                var dim = int.Parse(comboBoxDIM.Text);
                var options = new MdsEngine.Options {
                    Dimensions = dim,
                    LearnRate = lr,
                    NGenerations = gen,
                    PointUpdateStyle = (int)pus,
                    StartAnnealingWhen = sa,
                };
                return options;
            }
        }

        SimilarityFormat SelectedFormat { get { return (SimilarityFormat)comboBoxFMT.SelectedItem; } }
        MdsEngine.FormatAndOptions SelectedFormatAndOptions { get { return new MdsEngine.FormatAndOptions { Format = SelectedFormat, Options = SelectedOptions }; } }

        private void loadButton_Click(object sender, RoutedEventArgs e) {
            try {
                LoadSettings(SelectedFormatAndOptions, (g) => { });
            } catch (Exception e0) {
                Console.WriteLine(e0.StackTrace);
            }
        }

        void LoadSettings(MdsEngine.FormatAndOptions foptions, Action<GraphControl> whenDone) {
            GraphControl testGraph;
            if (PlotControl.TryGetGraph("test_" + foptions.ToString(), out testGraph)) {
                Console.WriteLine("Already Loaded.");
                return;
            }
            ThreadPool.QueueUserWorkItem((o) => {
                try {
                    var engine = new MdsEngine(settings.WithFormat(foptions.Format), null, null, foptions.Options);
                    if (!engine.ResultsAlreadyCached) {
                        Console.WriteLine("These results are not available");
                    } else {
                        engine.LoadCachedMds();
                        Console.WriteLine("Loaded.");
                        Dispatcher.BeginInvoke((Action)delegate {
                            try {
                                ShowMdsGraphs(foptions, engine, whenDone);
                            } catch (Exception e2) {
                                Console.WriteLine(e2.StackTrace);
                            }
                        });
                    }

                } catch (Exception e1) {
                    Console.WriteLine(e1.StackTrace);
                }

            });
        }

        void ShowMdsGraphs(MdsEngine.FormatAndOptions foptions, MdsEngine engine, Action<GraphControl> whenDone) {
            GraphControl oldGraph;
            if (PlotControl.TryGetGraph("test_" + foptions.ToString(), out oldGraph)) {
                PlotControl.Remove(oldGraph);
            }
            if (PlotControl.TryGetGraph("corr_" + foptions.ToString(), out oldGraph)) {
                PlotControl.Remove(oldGraph);
            }
            var testG = PlotControl.NewGraph("test_" + engine.resultsFilename, engine.TestSetRankings);
            var corrG = PlotControl.NewGraph("corr_" + engine.resultsFilename, engine.Correlations);
            testG.GraphBounds = new Rect(-0.005, 0.495, 1.01, 0.51);
            testG.XLabel = "MDS progress";
            testG.YLabel = "Average Test Ranking";
            corrG.GraphBounds = new Rect(-0.005, -0.005, 1.01, 1.01);
            corrG.XLabel = "MDS progress";
            corrG.YLabel = "Correlation to training (" + foptions.Options.Dimensions + "d)";
            PlotControl.ShowGraph(testG);
            PlotControl.ShowGraph(corrG);
            whenDone(testG);
            whenDone(corrG);
        }

        private void loadAllExportButton_Click(object sender, RoutedEventArgs e) {
            var fopts =
                from fmt in fmtValues
                from lr in lrValues
                from sa in saValues
                from pus in pusValues
                from gen in genValues
                from dim in Enumerable.Range(1, 51)
                select new MdsEngine.FormatAndOptions {
                    Format = fmt,
                    Options = new MdsEngine.Options {
                        Dimensions = dim,
                        LearnRate = lr,
                        NGenerations = gen,
                        PointUpdateStyle = (int)pus,
                        StartAnnealingWhen = sa,
                    }
                };
            foreach (var fopt in fopts)
                LoadSettings(fopt, (g) => {
                    try {
                        using (var stream = File.Open(@"C:\out\g-" + g.Name + ".xps", FileMode.Create, FileAccess.ReadWrite))
                            PlotControl.Print(g, stream);
                        PlotControl.Remove(g);
                    } catch (Exception ex) {
                        Console.WriteLine(ex.StackTrace);
                    }
                });
        }

        private void calculateButton_Click(object sender, RoutedEventArgs e) {
            progress.NewTask("Initializing...");
            var foptions = SelectedFormatAndOptions;
            var setup = new DistFormatLoader(new TimingProgressManager(), foptions.Format);
            GraphControl testG,
                corrG;
            ThreadPool.QueueUserWorkItem((o) => {
                try {
                    progress.NewTask("Loading data...");
                    setup.Init();
                    var engine = setup.ConstructMdsEngine(foptions.Options);
                    Dispatcher.Invoke((Action)delegate {
                        try {
                            ShowMdsGraphs(foptions, engine, (g) => { });
                        } catch (Exception e2) {
                            Console.WriteLine(e2.StackTrace);
                        }
                    });
                    PlotControl.TryGetGraph("test_" + foptions.ToString(), out testG);
                    PlotControl.TryGetGraph("corr_" + foptions.ToString(), out corrG);

                    engine.Correlations.CollectionChanged += delegate(object coll, NotifyCollectionChangedEventArgs eArgs) {
                        if (eArgs.Action == NotifyCollectionChangedAction.Add) {
                            foreach (Point p in eArgs.NewItems)
                                corrG.Dispatcher.BeginInvoke((Action<Point>)corrG.AddPoint, p);
                        }
                    };
                    engine.TestSetRankings.CollectionChanged += delegate(object coll, NotifyCollectionChangedEventArgs eArgs) {
                        if (eArgs.Action == NotifyCollectionChangedAction.Add) {
                            foreach (Point p in eArgs.NewItems)
                                testG.Dispatcher.BeginInvoke((Action<Point>)testG.AddPoint, p);
                        }
                    };

                    engine.DoMds(progress);

                    if (!engine.ResultsAlreadyCached)
                        engine.SaveMds();

                } catch (Exception e1) {
                    Console.WriteLine(e1.StackTrace);
                }

            });




        }

        void Correlations_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            throw new NotImplementedException();
        }

        private void exportButton_Click(object sender, RoutedEventArgs e) {
            try {
                var graph = graphPrintBox.SelectedItem as GraphControl;
                if (graph == null)
                    return;

                var saveDialog = new SaveFileDialog() {
                    Title = "Save Graph As ...",
                    Filter = "XPS file|*.xps",
                    FileName = graph.Name + ".xps",
                };
                if (saveDialog.ShowDialog() == true) {
                    using (var writestream = new FileStream(saveDialog.FileName, FileMode.Create, FileAccess.ReadWrite))
                        PlotControl.Print(graph, writestream);
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.StackTrace);
            }
        }

        void showSkreeGraph(Func<SkreePoint, double> makePoint, string shortname, string ylab, string xlab, SkreeGraph skreegraph, double? ymin, double? ymax) {
            var graph = PlotControl.NewGraph(shortname + "_" + skreegraph.opts.ToString(), skreegraph.points.Select(p => new Point(p.SkreeVariable, makePoint(p))));
            graph.YLabel = ylab;
            graph.XLabel = xlab;
            if (ymax.HasValue && ymin.HasValue) {
                Rect bounds = graph.GraphBounds;
                bounds.Y = ymin.Value;
                bounds.Height = (double)(ymax - ymin);
                graph.GraphBounds = bounds;
            }
            PlotControl.ShowGraph(graph);
        }

        private void loadSkreeGraphsButton_Click(object sender, RoutedEventArgs e) {
            new Thread((ThreadStart)delegate {
                SongDatabaseConfigFile config = new SongDatabaseConfigFile(false);
                var availOpts = MdsEngine.FormatAndOptions.AvailableInCache(config).ToArray();

                var skreeByDims = SkreePlot(
                    fopt => (double)fopt.Options.Dimensions,
                    fopt => { fopt.Options.Dimensions = 0; return fopt; },
                    availOpts, 15, 10);

                var skreeByNGens = SkreePlot(
                    fopt => (double)fopt.Options.NGenerations,
                    fopt => { fopt.Options.NGenerations = 0; return fopt; },
                    availOpts, 15, 10);

                var skreeByLRs = SkreePlot(
                    fopt => (double)fopt.Options.LearnRate,
                    fopt => { fopt.Options.LearnRate = 0; return fopt; },
                    availOpts, 15, 5);

                var skreeByPUs = SkreePlot(
                    fopt => (double)fopt.Options.PointUpdateStyle,
                    fopt => { fopt.Options.PointUpdateStyle = 0; return fopt; },
                    availOpts, 1500, 3);

                var skreeByFMTs = SkreePlot(
                    fopt => (double)(int)fopt.Format,
                    fopt => { fopt.Format = (SimilarityFormat)0; return fopt; },
                    availOpts, 1500, 4);


                Dispatcher.Invoke((Action)delegate {
                    foreach (var skreegraph in skreeByDims) {
                        showSkreeGraph(sp => sp.Correlation, "CorrByDim", "Correlation", "Dimensions", skreegraph, 0.5, 1.0);
                        showSkreeGraph(sp => sp.TestSetRanking, "TestRankByDim", "Mean Test Pair Rank", "Dimensions", skreegraph, 0.7, 1.0);
                    }

                    foreach (var skreegraph in skreeByNGens) {
                        showSkreeGraph(sp => sp.Correlation, "CorrByGen", "Correlation", "Generations", skreegraph, 0.5, 1.0);
                        showSkreeGraph(sp => sp.TestSetRanking, "TestRankByGen", "Mean Test Pair Rank", "Generations", skreegraph, 0.7, 1.0);
                    }
                    foreach (var skreegraph in skreeByLRs) {
                        showSkreeGraph(sp => sp.Correlation, "CorrByLR", "Correlation", "Learning Rate", skreegraph, 0.5, 1.0);
                        showSkreeGraph(sp => sp.TestSetRanking, "TestRankByLR", "Mean Test Pair Rank", "Learning Rate", skreegraph, 0.7, 1.0);
                    }

                    Func<SkreeGraph, SkreeGraph, Func<double, double, double>, SkreeGraph> combineGraphs =
                        (g1, g2, comb) => new SkreeGraph {
                            opts = g1.opts,
                            points = g1.points.ZipWith(g2.points, (p1, p2) =>
                                new SkreePoint {
                                    SkreeVariable = p1.SkreeVariable,
                                    Correlation = comb(p1.Correlation, p2.Correlation),
                                    TestSetRanking = comb(p1.TestSetRanking, p2.TestSetRanking)
                                })
                        };


                    foreach (var skreegraph in new[] { skreeByPUs.Aggregate((g1, g2) => combineGraphs(g1, g1, Math.Max)) }) {
                        showSkreeGraph(sp => sp.Correlation, "MaxCorrByPU", "MaxCorrelation", "PointUpdate", skreegraph, null, null);
                        showSkreeGraph(sp => sp.TestSetRanking, "MaxTestRankByPU", "Max Mean Test Pair Rank", "PointUpdate", skreegraph, null, null);
                    }
                    foreach (var skreegraph in new[] { skreeByFMTs.Aggregate((g1, g2) => combineGraphs(g1, g1, Math.Max)) }) {
                        showSkreeGraph(sp => sp.Correlation, "MaxCorrByFMT", "MaxCorrelation", "Format", skreegraph, null, null);
                        showSkreeGraph(sp => sp.TestSetRanking, "MaxTestRankByFMT", "Max Mean Test Pair Rank", "Format", skreegraph, null, null);
                    }
                    foreach (var skreegraph in new[] { skreeByPUs.Aggregate((g1, g2) => combineGraphs(g1, g1, (a, b) => a + b)) }) {
                        showSkreeGraph(sp => sp.Correlation, "CorrByPU", "Correlation", "PointUpdate", skreegraph, null, null);
                        showSkreeGraph(sp => sp.TestSetRanking, "TestRankByPU", "Mean Test Pair Rank", "PointUpdate", skreegraph, null, null);
                    }
                    foreach (var skreegraph in new[] { skreeByFMTs.Aggregate((g1, g2) => combineGraphs(g1, g1, (a, b) => a + b)) }) {
                        showSkreeGraph(sp => sp.Correlation, "CorrByFMT", "Correlation", "Format", skreegraph, null, null);
                        showSkreeGraph(sp => sp.TestSetRanking, "TestRankByFMT", "Mean Test Pair Rank", "Format", skreegraph, null, null);
                    }
                });

            }) { IsBackground = true }.Start();
        }

        struct SkreePoint { public double Correlation, TestSetRanking, SkreeVariable;        }
        struct SkreeGraph { public MdsEngine.FormatAndOptions opts; public IEnumerable<SkreePoint> points;        }
        IEnumerable<SkreeGraph> SkreePlot(Func<MdsEngine.FormatAndOptions, double> selectKey,
            Func<MdsEngine.FormatAndOptions, MdsEngine.FormatAndOptions> ignoreKey,
            MdsEngine.FormatAndOptions[] availOpts, int maxPlots, int minItems) {
            return (
                from g in
                    (from fopt in availOpts
                     group fopt by ignoreKey(fopt) into g
                     let count = g.Count()
                     where count >= minItems
                     orderby count descending
                     select g).Take(maxPlots)
                select new SkreeGraph {
                    opts = g.Key,
                    points = (from spOpt in g
                              let selkey = selectKey(spOpt)
                              orderby selkey
                              let cachedMds = new MdsEngine(settings.WithFormat(spOpt.Format), null, null, spOpt.Options)
                              select new SkreePoint {
                                  SkreeVariable = selkey,
                                  Correlation = cachedMds.LoadOnlyFinalCorrelation(),
                                  TestSetRanking = cachedMds.LoadOnlyFinalTestSetRanking(),
                              }).ToArray()
                }).ToArray();
        }

        private void exportAllLoaded_Click(object sender, RoutedEventArgs e) {
            foreach (GraphControl g in PlotControl.Graphs) {

                using (var stream = File.Open(@"C:\out\g-" + g.Name + ".xps", FileMode.Create, FileAccess.ReadWrite))
                    PlotControl.Print(g, stream);

            }
        }

        private void unloadButton_Click(object sender, RoutedEventArgs e) {
            var graph = graphPrintBox.SelectedItem as GraphControl;
            if (graph == null)
                return;
            PlotControl.Remove(graph);
        }

        private void unloadAllButton_Click(object sender, RoutedEventArgs e) { PlotControl.Graphs.Clear(); }

        private void listBestButton_Click(object sender, RoutedEventArgs e) {
            new Thread((ThreadStart)delegate {
                SongDatabaseConfigFile config = new SongDatabaseConfigFile(false);
                var availOpts = MdsEngine.FormatAndOptions.AvailableInCache(config).ToArray();
                Console.WriteLine("Running query...");
                var query = from fopt in availOpts

                            let cachedMds = new MdsEngine(settings.WithFormat(fopt.Format), null, null, fopt.Options)
                            group new { fopt, cachedMds } by fopt.Options.Dimensions into g
                            from bestOf in
                                (from optengine in g
                                 let correlation = optengine.cachedMds.LoadOnlyFinalCorrelation()
                                 let testRanking = optengine.cachedMds.LoadOnlyFinalTestSetRanking()
                                 orderby testRanking + 0.1 * correlation descending
                                 select new { Fopt = optengine.fopt, Corr = correlation, TestRank = testRanking }
                                    ).Take(5)
                            orderby bestOf.TestRank + 0.1 * bestOf.Corr descending
                            select bestOf;
                bool first = true;
                foreach (var result in query.Take(100)) {
                    Console.WriteLine("DIM:{3,2}, TR:{0:f4}, Corr:{1:f4}, {2}", result.TestRank, result.Corr, result.Fopt, result.Fopt.Options.Dimensions);
                    if (first) {
                        first = false;
                        Dispatcher.Invoke((Action)delegate {
                            comboBoxDIM.Text = result.Fopt.Options.Dimensions.ToString();
                            comboBoxFMT.SelectedItem = result.Fopt.Format;
                            comboBoxGEN.Text = result.Fopt.Options.NGenerations.ToString();
                            comboBoxLR.Text = result.Fopt.Options.LearnRate.ToString();
                            comboBoxPUS.SelectedItem = (PointUpdate) result.Fopt.Options.PointUpdateStyle;
                            comboBoxSA.Text = result.Fopt.Options.StartAnnealingWhen.ToString();
                        });
                    }
                }

            }) { IsBackground = true }.Start();
        }

        private void hideAllButton_Click(object sender, RoutedEventArgs e) {
            foreach (var graph in PlotControl.Graphs) graph.Visibility = Visibility.Collapsed;
        }

        private void triangulateButton_Click(object sender, RoutedEventArgs e) {
            var fopts = SelectedFormatAndOptions;
            new Thread((ThreadStart)delegate {
                DistFormatLoader loader = new DistFormatLoader(this.progress, fopts.Format);
                loader.Init();
                MdsEngine engine =  loader.ConstructMdsEngine(fopts.Options);
                engine.LoadCachedMds();
                var sqliteToAll = loader.Settings.LoadTrackMapper();
                progress.NewTask("Embedding");
                
                var allDists = loader.Settings.AllTracksCached
                    .Where( nf=> loader.CachedMatrix.Mapping.IsMapped(nf.number))
                    .Select(nf=>
                        new EmbedNonLandmarks.DistanceToLandmark {
                            LandmarkIndex = loader.CachedMatrix.Mapping.GetMap(nf.number),
                            DistanceToAllSongs = loader.CachedMatrix.LoadDistsFromId(nf.number)
                        })
                        .InAnotherThread(10);

                double[,] allPositions=
                    EmbedNonLandmarks.Triangulate(progress, sqliteToAll.Count, loader.CachedMatrix.Matrix, engine.MdsResults, 
                    allDists
                    );
                progress.Done();
                FileInfo saveFile = new FileInfo(System.IO.Path.Combine(loader.Settings.DataDirectory.FullName, @".\pos-" + fopts + ".pos"));
                progress.NewTask("Saving: "+saveFile.Name);
                using (var stream = saveFile.Open(FileMode.Create, FileAccess.Write))
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write((int)allPositions.GetLength(0)); //# of points
                    writer.Write((int)allPositions.GetLength(1)); //# of dims
                    for(int pi=0;pi<allPositions.GetLength(0);pi++)
                        for (int dim = 0; dim < allPositions.GetLength(1); dim++) 
                            writer.Write((double)allPositions[pi, dim]);
                    sqliteToAll.WriteTo(writer);//then we write mapping between sqlite id's and this id's scheme.
                }
                progress.Done();

            }) { IsBackground = true }.Start();

        }


    }
}
