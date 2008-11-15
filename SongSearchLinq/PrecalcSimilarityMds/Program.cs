using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimilarityMdsLib;
using System.Threading;
using EmnExtensions;
using EmnExtensions.Algorithms;
using LastFMspider;
using SongDataLib;
namespace PrecalcSimilarityMds
{
    class Program
    {
        static void Main(string[] args) {
           InitializeCalculationBackend();
        //    FixBorkedMdses();
            LoadInParallel();
        }
        static Dictionary<SimilarityFormat, DistFormatLoader> progs;
        static SongDatabaseConfigFile config = new SongDatabaseConfigFile(false);
        static LastFmTools tools = new LastFmTools(config);
        static SimCacheManager settings = new SimCacheManager(SimilarityFormat.LastFmRating, tools, DataSetType.Training);

        static void InitializeCalculationBackend() {
            var programAvRank2 = new DistFormatLoader(new TimingProgressManager(), SimilarityFormat.AvgRank2);
            var programAvRank = new DistFormatLoader(new TimingProgressManager(), SimilarityFormat.AvgRank);
            var programLog200 = new DistFormatLoader(new TimingProgressManager(), SimilarityFormat.Log200);
            var programLog2000 = new DistFormatLoader(new TimingProgressManager(), SimilarityFormat.Log2000);
            progs = new[] { programAvRank2, programLog2000, programLog200, programAvRank }
                .ToDictionary(prog => prog.format, prog => prog);
            Parallel.ForEach(
            progs, prog => {
                prog.Value.Init();
            });

        }

        static void FixBorkedMdses() {
            DateTime border = new DateTime(2008, 11, 12, 18, 50, 00);
            foreach (var fopts in MdsEngine.FormatAndOptions.AvailableInCache(config)) {
                var engine=new MdsEngine(settings.WithFormat(fopts.Format),null,null,fopts.Options );
                if (engine.mdsFile.LastWriteTime > border) {
                    if (fopts.Options.LearnRate == 2.0 || fopts.Options.LearnRate == 1.0) {
                        Console.WriteLine("Deleting " + fopts);
                        engine.mdsFile.Delete();
                        engine.testFile.Delete();
                        engine.corrFile.Delete();
                    } else if (fopts.Options.LearnRate == 0.0) {
                        Console.WriteLine("Moving " + fopts);
                        MdsEngine.Options newopts = fopts.Options;
                        newopts.LearnRate = 1.0 / Math.Sqrt(2);
                        MdsEngine newengine = new MdsEngine(settings.WithFormat(fopts.Format), null, null, newopts);
                        engine.corrFile.MoveTo(newengine.corrFile.FullName);
                        engine.testFile.MoveTo(newengine.testFile.FullName);
                        engine.mdsFile.MoveTo(newengine.mdsFile.FullName);
                    }


                } else {
                    double corr = engine.LoadOnlyFinalCorrelation();
                    double test = engine.LoadOnlyFinalTestSetRanking();
                    if (corr >= 1.0) {
                        Console.WriteLine("Hmm: {0}, Corr: {1}", fopts, corr);
                        Console.WriteLine("Deleting " + fopts);
                        engine.mdsFile.Delete();
                        engine.testFile.Delete();
                        engine.corrFile.Delete();

                    }
                }
            }
        }

        static void LoadInParallel() {


            Semaphore sem = new Semaphore(5, 5);
            var optsChoices = MakeInterestingOptions().ToArray();
            Parallel.ForEach(optsChoices, opts => {
                bool waited = false;
                try {
                    sem.WaitOne();//to avoid overusing memory...
                    waited = true;
                    progs[opts.Key].Run(opts.Value, false);
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
                from dims in new[] { 2, 10 }
                from gen in new[] { 20, 50 }
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
            var forLessDecent =
                 from lessDecentFormat in new[] { SimilarityFormat.AvgRank, SimilarityFormat.Log200 }
                 from sa in new[] { 0.0, 0.5 }
                 from lr in new[] { 1.0, 5.0 }
                 from pus in new[] { 1, 2, 0 }
                 from dims in new[] { 2, 10 }
                 from gen in new[] { 50 }
                 select new KeyValuePair<SimilarityFormat, MdsEngine.Options>(
                     lessDecentFormat,
                     new MdsEngine.Options {
                         Dimensions = dims,
                         PointUpdateStyle = pus,
                         StartAnnealingWhen = sa,
                         LearnRate = lr,
                         NGenerations = gen
                     }
                 );
            var forSkreePlotN =
                 from decentFormat in new[] { SimilarityFormat.AvgRank2, SimilarityFormat.Log2000 }
                 from sa in new[] { 0.0 }
                 from lr in new[] { 1.0, 2.0 }
                 from pus in new[] { 1, 2 }
                 from dims in new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 14, 16, 18, 20, 23, 26, 29, 32, 36, 40, 45, 50 }
                 from gen in new[] { 30,80 }
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
            var forSkreePlotLR =
                 from decentFormat in new[] { SimilarityFormat.AvgRank2, SimilarityFormat.Log2000 }
                 from sa in new[] { 0.0 }
                 from lr in new[] { 1.0 / s2, 1.0, s2, 2.0, s2 *2.0, 4.0, s2 * 4.0, 8.0 } 
                 from pus in new[] { 1, 2 }
                 from dims in new[] { 2, 10, }
                 from gen in new[] { 40, }
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
            var forSkreePlotGEN =
                 from decentFormat in new[] { SimilarityFormat.AvgRank2, SimilarityFormat.Log2000 }
                 from sa in new[] { 0.0 }
                 from lr in new[] { 1.0, 2.0, }
                 from pus in new[] { 1, 2 }
                 from dims in new[] { 2, 10, }
                 from gen in new[] { 10, 15, 20, 25, 30, 40, 50, 80,160 }
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
            var forGoodResult =
                 from decentFormat in new[] { SimilarityFormat.AvgRank2, SimilarityFormat.Log2000 }
                 from sa in new[] { 0.0 }
                 from lr in new[] { 2.0, }
                 from gen in new[] { 160,150,140 }
                 from pus in new[] { 1, 2 }
                 from dims in new[] { 2,5, 10,20 }
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
            return forGoodResult.Concat(forSkreePlotN).Concat(forSkreePlotGEN).Concat(forSkreePlotLR).Concat(forDecent).Concat(forLessDecent);
        }

    }
}
