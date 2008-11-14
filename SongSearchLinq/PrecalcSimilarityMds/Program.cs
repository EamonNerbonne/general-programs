using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimilarityMdsLib;
using System.Threading;
using EmnExtensions;
using EmnExtensions.Algorithms;
using LastFMspider;
namespace PrecalcSimilarityMds
{
    class Program
    {
        static void Main(string[] args) {
            LoadInParallel();
        }

        static void LoadInParallel() {
            var programAvRank2 = new DistFormatLoader(new TimingProgressManager(), SimilarityFormat.AvgRank2);
            var programAvRank = new DistFormatLoader(new TimingProgressManager(), SimilarityFormat.AvgRank);
            var programLog200 = new DistFormatLoader(new TimingProgressManager(), SimilarityFormat.Log200);
            var programLog2000 = new DistFormatLoader(new TimingProgressManager(), SimilarityFormat.Log2000);
            var progs = new[] { programAvRank2, programLog2000, programLog200, programAvRank }
                .ToDictionary(prog => prog.format, prog => prog);
            Parallel.ForEach(
            progs, prog => {
                prog.Value.Init();
            });

            Semaphore sem = new Semaphore(5, 5);
            var optsChoices = MakeInterestingOptions().ToArray();
            optsChoices.Shuffle();
            Parallel.ForEach(optsChoices, opts => {
                bool waited = false;
                try {
                    sem.WaitOne();//to avoid overusing memory...
                    waited = true;
                    progs[opts.Key].Run(opts.Value,false);
                } catch (Exception e) {
                    Console.WriteLine(e.StackTrace);
                } finally {
                    if (waited)
                        sem.Release();
                }
            });

        }

        static IEnumerable<KeyValuePair<SimilarityFormat, MdsEngine.Options>> MakeInterestingOptions() {
            var forDecent =
                from decentFormat in new[] { SimilarityFormat.AvgRank2, SimilarityFormat.Log2000 }
                from sa in new[] { 0.0, 0.5 }
                from lr in new[] { 1.0, 5.0, 2.0 }
                from pus in new[] { 1, 2, 0 }
                from dims in new[] { 2, 10, 50 }
                from gen in new[] { 20, 50 }
                select new KeyValuePair<SimilarityFormat, MdsEngine.Options> (
                    decentFormat,
                    new MdsEngine.Options {
                        Dimensions = dims,
                        PointUpdateStyle = pus,
                        StartAnnealingWhen = sa,
                        LearnRate = lr,
                        NGenerations = gen
                    }
                );
            var forLessDecent =
                 from lessDecentFormat in new[] { SimilarityFormat.AvgRank, SimilarityFormat.Log200 }
                 from sa in new[] { 0.0, 0.5 }
                 from lr in new[] { 1.0, 5.0 }
                 from pus in new[] { 1, 2, 0 }
                 from dims in new[] { 2, 10 }
                 from gen in new[] { 50 }
                 select new KeyValuePair<SimilarityFormat, MdsEngine.Options> (
                     lessDecentFormat,
                     new MdsEngine.Options {
                         Dimensions = dims,
                         PointUpdateStyle = pus,
                         StartAnnealingWhen = sa,
                         LearnRate = lr,
                         NGenerations = gen
                     }
                 );
            var forSkreePlot =
                 from decentFormat in new[] { SimilarityFormat.AvgRank2, SimilarityFormat.Log2000 }
                 from sa in new[] { 0.0 }
                 from lr in new[] { 1.0,2.0 }
                 from pus in new[] { 1, 2 }
                 from dims in new[] { 1,2,3,4,5,6,7,8,9,10,12,14,16,18,20,23,26,29,32,36,40,45,50 }
                 from gen in new[] { 30 }
                 select new KeyValuePair<SimilarityFormat, MdsEngine.Options>(
                     decentFormat,
                     new MdsEngine.Options {
                         Dimensions = dims,
                         PointUpdateStyle = pus,
                         StartAnnealingWhen = sa,
                         LearnRate = lr,
                         NGenerations = gen
                     }
                 );
            double s2 = Math.Sqrt(2);
            var forSkreePlot2 =
                 from decentFormat in new[] { SimilarityFormat.AvgRank2, SimilarityFormat.Log2000 }
                 from sa in new[] { 0.0 }
                 from lr in new[] { 1.0/s2,1.0,s2, 2.0,2*s2,4.0,2*4.0,8.0 }
                 from pus in new[] { 1, 2 }
                 from dims in new[] { 2,5,  10, }
                 from gen in new[] { 10,15,20,25,30,35,40,45,50,60,100 }
                 select new KeyValuePair<SimilarityFormat, MdsEngine.Options>(
                     decentFormat,
                     new MdsEngine.Options {
                         Dimensions = dims,
                         PointUpdateStyle = pus,
                         StartAnnealingWhen = sa,
                         LearnRate = lr,
                         NGenerations = gen
                     }
                 );
            return forSkreePlot.Concat(forDecent.Concat(forLessDecent)).Concat(forSkreePlot2);
        }

    }
}
