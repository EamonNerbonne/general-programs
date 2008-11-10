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

namespace RealSimilarityMds
{
    public partial class MusicMdsDisplay : Window
    {
        ProgressManager progress;
        //Program program;
        public PlotControl HistogramControl { get { return this.distanceHistoview; } }

        internal GraphControl corrgraph, testSetRanking;
        public MusicMdsDisplay() {
            InitializeComponent();

            progress = new ProgressManager(progressBar, labelETA, new NiceTimer());
            corrgraph = HistogramControl.NewGraph("corr", new Point[] { });
            corrgraph.EnsurePointInBounds(new Point(0, 0));
            corrgraph.EnsurePointInBounds(new Point(1, 1));
            HistogramControl.ShowGraph(corrgraph);

            testSetRanking = HistogramControl.NewGraph("testRank", new Point[] { });
            testSetRanking.EnsurePointInBounds(new Point(0, 0.5));
            testSetRanking.EnsurePointInBounds(new Point(1, 1));
            HistogramControl.ShowGraph(testSetRanking);




            new Thread(InBg) {
                IsBackground = true,
            }.Start();
        }

        void InBg() {
            var programAvRank2 = new Program(progress, this, SimilarityFormat.AvgRank2);
            var programAvRank = new Program(progress, this, SimilarityFormat.AvgRank);
            var programLog200 = new Program(progress, this, SimilarityFormat.Log200);
            var programLog2000 = new Program(progress, this, SimilarityFormat.Log2000);
            var progs = new[] { programAvRank2, programLog2000, programLog200, programAvRank };
            Parallel.ForEach(
            progs, prog => {
                prog.Init();
            });

            Semaphore sem = new Semaphore(5, 5);
            var optsChoices=MakeInterestingOptions().ToArray();
            optsChoices.Shuffle();
            Parallel.ForEach(optsChoices, opts => {
                foreach (var prog in progs) {
                    bool dubious = ((opts.StartAnnealingWhen == 0.5 || opts.Dimensions == 10 || opts.Dimensions == 50) && (prog.format == SimilarityFormat.AvgRank || prog.format == SimilarityFormat.Log200));
                    if (!dubious) try {
                            sem.WaitOne();//for memory issues...
                            prog.Run(opts);
                            sem.Release();
                        } catch (Exception e) { Console.WriteLine(e.StackTrace); }
                }
            });

        }

        IEnumerable<RealSimilarityMds.MdsEngine.Options> MakeInterestingOptions() {
            MdsEngine.Options opts;
            opts.NGenerations = 30;
            opts.LearnRate = 2.0;
            opts.StartAnnealingWhen = 0.0;
            opts.PointUpdateStyle = 1;
            opts.Dimensions = 2;



            yield return opts;
            foreach (var sa in new[] { 0.0, 0.5 })
                foreach (var lr in new[] { 1.0, 5.0, 2.0 })
                    foreach (var pus in new[] { 1, 2, 0 })
                        foreach (var dims in new[] { 2, 3, 5, 10, 20, 50 })
                            foreach (var gen in new[] { 20, 50 }) 
                                yield return new MdsEngine.Options {
                                    Dimensions = dims,
                                    PointUpdateStyle = pus,
                                    StartAnnealingWhen = sa,
                                    LearnRate = lr,
                                    NGenerations = gen
                                };
        }

    }
}
