using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using LastFMspider;
using LastFMspider.LastFMSQLiteBackend;
using EmnExtensions;
using System.IO;
using System.Data.Common;
using System.Diagnostics;
using System.Collections;
using System.IO.Compression;

namespace SimilarityAnalysis
{



    static class Program
    {

        static ulong ID(SimilarTrackRow row) {
            uint a = (uint)row.TrackA;
            uint b = (uint)row.TrackB;
            uint tmp;
            if (a < b) {
                tmp = a;
                a = b;
                b = tmp;
            }
            return (((ulong)a) << 32) | ((ulong)b);
        }

        static ulong[] UniqueSimilarities(SimilarTracks sims) {
            return UniqueSimilarities((countPush, itemPush) => {
                countPush(sims.CountOneSided);
                int i = 0;
                foreach (var item in sims.SimilaritiesOneSidedSqlite)
                    itemPush(i++, item);
            });
        }

        static ulong[] UniqueSimilarities(Action<Func<int,int> , Action <int,SimilarTrackRow> > srcOfRows) {
            ulong[] retval=null;
            srcOfRows(
                (n) => { retval = new ulong[n]; return n; },
                (i, row) => { retval[i] = ID(row); }
            );
            NiceTimer.Time("sorting...", () => { Array.Sort(retval); });

            int lastWritePos = 0;
            ulong last = retval[0];
            for (int i = 1; i < retval.Length; i++) {
                if (retval[i] != last) {
                    lastWritePos++;
                    retval[lastWritePos] = retval[i]; //might be retval[x]=retval[x];
                }
            }
            Array.Resize(ref retval, lastWritePos + 1);
            return retval;
        }

        static IEnumerable<ulong> Merge(IEnumerable<ulong> a, IEnumerable<ulong> b) {
            var aEnum = a.GetEnumerator();
            var bEnum = b.GetEnumerator();
            bool aHasNext = aEnum.MoveNext();
            bool bHasNext = bEnum.MoveNext();
            ulong aVal=aHasNext? aEnum.Current:0;
            ulong bVal=bHasNext? bEnum.Current:0;

            while (aHasNext&&bHasNext) {
                if (aVal < bVal) {
                    yield return aVal;
                    aHasNext = aEnum.MoveNext();
                    aVal=aHasNext? aEnum.Current:0;
                } else {
                    yield return bVal;
                    bHasNext = bEnum.MoveNext();
                    bVal=bHasNext? bEnum.Current:0;
                }
            }
            if (bHasNext) {
                yield return bVal;
                while (bEnum.MoveNext()) yield return bEnum.Current;
            } else if (aHasNext) {
                yield return aVal;
                while (aEnum.MoveNext()) yield return aEnum.Current;
            }
        }

        //sparseness
        static void Main(string[] args) {
            SongDatabaseConfigFile config = new SongDatabaseConfigFile(false);
            LastFmTools tools = new LastFmTools(config);
            //Dictionary<SimilarTracks.DataSetType, int> simCounts = new Dictionary<SimilarTracks.DataSetType, int>();
            //Dictionary<SimilarTracks.DataSetType, double> simSums = new Dictionary<SimilarTracks.DataSetType, double>();
            
            NiceTimer timer = new NiceTimer();
            /*foreach (var dataSetType in new[] { SimilarTracks.DataSetType.Complete }){ //, SimilarTracks.DataSetType.Test, SimilarTracks.DataSetType.Verification, SimilarTracks.DataSetType.Training }) {
                timer.TimeMark("Loading: " + dataSetType);
                var sims = SimilarTracks.LoadOrCache(tools, dataSetType).Similarities;
                timer.TimeMark("Counting: " + dataSetType);
                simCounts[dataSetType] = sims.Count();
                simSums[dataSetType] = sims.Select(row => (double)row.Rating).Sum();
                Console.WriteLine("{0} contains {1} similarities totalling {2}.", dataSetType, simCounts[dataSetType], simSums[dataSetType]);
            }
            timer.TimeMark(null);*/
            //Console.WriteLine("Sum of all non-complete types is {0}.", simSums.Where(sums => sums.Key != SimilarTracks.DataSetType.Complete).Sum(sums=>sums.Value) );
            //Console.WriteLine("Count of all non-complete types is {0}.", simCounts.Where(sums => sums.Key != SimilarTracks.DataSetType.Complete).Sum(sums => sums.Value));


            Console.WriteLine("Verifying that all sets are subsets of DB.");

            timer.TimeMark("loading DB set");
            var fromDB = UniqueSimilarities( (countPush,itemPush)=>{
                tools.SimilarSongs.backingDB.RawSimilarTracks.Execute(true,countPush,itemPush);
            });

            timer.TimeMark("loading complete set");
            var completeSet = UniqueSimilarities(SimilarTracks.LoadOrCache(tools, SimilarTracks.DataSetType.Complete));

            timer.TimeMark("Comparing...");
            Console.Write("DB == Complete? ");Console.WriteLine(completeSet.SequenceEqual(completeSet));
            fromDB = null;


            timer.TimeMark("Loading Test...");
            var testSet = UniqueSimilarities(SimilarTracks.LoadOrCache(tools, SimilarTracks.DataSetType.Test));

            timer.TimeMark("Loading Training...");
            var trainSet =  UniqueSimilarities(SimilarTracks.LoadOrCache(tools, SimilarTracks.DataSetType.Training));

            timer.TimeMark("Loading Verification...");
            var verSet =  UniqueSimilarities(SimilarTracks.LoadOrCache(tools, SimilarTracks.DataSetType.Verification));

            timer.TimeMark("Comparing...");
            var mergeSet = Merge(testSet, Merge(verSet, trainSet));

            Console.Write("Complete = Test+Train+Verification? "); Console.WriteLine(completeSet.SequenceEqual(mergeSet));





       //     double.PositiveInfinity
            /*
            LastFMSQLiteCache db = tools.SimilarSongs.backingDB;
            SimilarTrackRow[] similarTracks = SimilarTracks.LoadSimilarTracksFromDB(db, timer);

            TrackMap mapper = new TrackMap();
            DistanceMem dmem = new DistanceMem();
            mapper.AddedTrack = dmem.RegTrack;
            double offset = Math.Log(-100.0);
            timer.TimeMark("Inserting song similarity...");
            foreach (var relation in similarTracks) {
                int songA = mapper.Map(relation.TrackA),
                    songB = mapper.Map(relation.TrackB);
                float distance = -(float)(Math.Log(relation.Rating) + offset);
                dmem.AddDistance(songA, songB, distance);
            }
            long testCount = 0;
            long errCount = 0;
            double errAmount = 0;
            timer.TimeMark("Validating triangle inequality");
            using (var writer = File.OpenWrite("errlog.log"))
            using (var swriter = new StreamWriter(writer)) {
                DateTime lastUpdate = DateTime.Now;
                for (int a = 0; a < dmem.distances.Count; a++) {
                    foreach (int b in dmem.distances[a].Keys) {
                        if (a < b) break;
                        foreach (int c in dmem.distances[b].Keys) {
                            if (b < c) break;

                            if (dmem.distances[a].ContainsKey(c)) {
                                float ac = dmem.distances[a][c],
                                    ab = dmem.distances[a][b],
                                    bc = dmem.distances[b][c];

                                float longest = Math.Max(ac, Math.Max(ab, bc));
                                float rest = ac + ab + bc - longest;
                                testCount++;
                                if (longest > rest) {
                                    errCount++;
                                    errAmount += longest - rest;
                                    swriter.WriteLine("{3}: {0},{1},{2}: {4}/{5}",
                                        a, b, c, longest - rest, errCount, testCount);
                                }
                            }
                        }
                    }

                    if (DateTime.Now - lastUpdate > TimeSpan.FromSeconds(1)) {
                        Console.WriteLine("{0}/{1}", errCount, testCount);
                        lastUpdate = DateTime.Now;
                    }
                }
            }
            timer.TimeMark(null);
              */
        }
    }
}
