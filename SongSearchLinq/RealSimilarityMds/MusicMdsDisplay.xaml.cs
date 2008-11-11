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

namespace RealSimilarityMds
{
    public partial class MusicMdsDisplay : Window
    {
        ProgressManager progress;
        //Program program;
        public PlotControl HistogramControl { get { return this.distanceHistoview; } }

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
        }

        SimilarityFormat[] fmtValues = new[] { SimilarityFormat.AvgRank, SimilarityFormat.AvgRank2, SimilarityFormat.Log200, SimilarityFormat.Log2000 };
        double[] lrValues = new[] { 1.0, 5.0, 2.0 };
        double[] saValues = new[] { 0.0, 0.5 };
        PointUpdate[] pusValues = new[] { PointUpdate.ConstantRandom, PointUpdate.DecreasingRandom, PointUpdate.OriginalHiTmds };
        int[] genValues = new[] { 20, 30, 50 };
        int[] dimValues = new[] { 2, 3, 5, 10, 20, 50 };

        enum PointUpdate
        {
            ConstantRandom = 1,
            DecreasingRandom = 2,
            OriginalHiTmds = 0,
        }
        HashSet<KeyValuePair<SimilarityFormat, MdsEngine.Options>> loaded = new HashSet<KeyValuePair<SimilarityFormat, MdsEngine.Options>>();

        private void button1_Click(object sender, RoutedEventArgs e) {
            try {
                var fmt = (SimilarityFormat)comboBoxFMT.SelectedItem;
                var lr = (double)comboBoxLR.SelectedItem;
                var sa = (double)comboBoxSA.SelectedItem;
                var pus = (PointUpdate)comboBoxPUS.SelectedItem;
                var gen = (int)comboBoxGEN.SelectedItem;
                var dim = (int)comboBoxDIM. SelectedItem;
                var options = new MdsEngine.Options {
                    Dimensions = dim,
                    LearnRate = lr,
                    NGenerations = gen,
                    PointUpdateStyle = (int)pus,
                    StartAnnealingWhen = sa,
                };
                LoadSettings(fmt, options, (g) => { });
            } catch (Exception e0) {
                Console.WriteLine(e0.StackTrace);
            }
        }


        void PrintAll() {
            var opts =
                   from lr in lrValues
                   from sa in saValues
                   from pus in pusValues
                   from gen in genValues
                   from dim in Enumerable.Range(1,51)
                   select new MdsEngine.Options {
                       Dimensions = dim,
                       LearnRate = lr,
                       NGenerations = gen,
                       PointUpdateStyle = (int)pus,
                       StartAnnealingWhen = sa,
                   };
            foreach (var fmt in fmtValues)
                foreach (var opt in opts)
                    LoadSettings(fmt, opt, (g) => {
                        try {
                                using (var stream = File.Open(@"C:\out\g-" + g.Name + ".xps", FileMode.Create, FileAccess.ReadWrite))
                                    HistogramControl.Print(g, stream);
                        } catch (Exception ex) {
                            Console.WriteLine(ex.StackTrace);
                        }

                    });

        }
        void LoadSettings(SimilarityFormat fmt,MdsEngine.Options options, Action<GraphControl> whenDone) {
            if (loaded.Contains(new KeyValuePair<SimilarityFormat, MdsEngine.Options>(fmt, options))) {
                Console.WriteLine("Already Loaded.");
                return;
            }
            ThreadPool.QueueUserWorkItem((o) => {
                try {
                    var engine = new MdsEngine(settings.WithFormat(fmt), null, null, options);
                    if (!engine.ResultsAlreadyCached) {
                        Console.WriteLine("These results are not available");
                    } else {
                        engine.LoadCachedMds();
                        Console.WriteLine("Loaded.");
                        Dispatcher.BeginInvoke((Action)delegate {
                            try {
                                if (loaded.Contains(new KeyValuePair<SimilarityFormat, MdsEngine.Options>(fmt, options))) {
                                    Console.WriteLine("Already Loaded!");
                                    return;
                                }
                                var testG = HistogramControl.NewGraph("test_" + engine.resultsFilename, engine.TestSetRankings);
                                var corrG = HistogramControl.NewGraph("corr_" + engine.resultsFilename, engine.Correlations);
                                testG.GraphBounds = new Rect(-0.01, 0.49, 1.02, 0.52);
                                corrG.GraphBounds = new Rect(-0.01, -0.01, 1.02, 1.02);
                                HistogramControl.ShowGraph(testG);
                                HistogramControl.ShowGraph(corrG);
                                loaded.Add(new KeyValuePair<SimilarityFormat, MdsEngine.Options>(fmt, options));
                                whenDone(testG);
                                whenDone(corrG);
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

        private void button2_Click(object sender, RoutedEventArgs e) {
            PrintAll();
            /*
            try {
                foreach(var graph in HistogramControl.Graphs.Where(g=>g!=null)) {
                using(var stream =File.Open(@"C:\out\g-"+graph.Name+ ".xps", FileMode.Create, FileAccess.ReadWrite))
                HistogramControl.Print(graph,stream );
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.StackTrace);
            }
            */
        }


    }
}
