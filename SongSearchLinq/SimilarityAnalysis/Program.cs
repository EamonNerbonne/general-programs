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
                countPush(sims.Count);
                int i = 0;
                foreach (var item in sims.SimilaritiesSqlite)
                    itemPush(i++, item);
            });
        }

        static ulong[] UniqueSimilarities(Action<Func<int,int> , Action <int,SimilarTrackRow> > srcOfRows) {
            ulong[] retval=null;
            int lastWritePos = 0;
            srcOfRows(
                (n) => { retval = new ulong[n]; return n; },
                (i, row) => { if(row.TrackA!=row.TrackB) retval[lastWritePos++] = ID(row); }
            );
            int simNum = lastWritePos;
            NiceTimer.Time("sorting...", () => { Array.Sort(retval, 0, simNum); });

            lastWritePos = 0;
            ulong last = retval[0];
            for (int i = 1; i < simNum; i++) {
                if (retval[i] != last) {
                    last = retval[i];
                    lastWritePos++;
                    retval[lastWritePos] = last; //might be retval[x]=retval[x];
                }
            }
            Array.Resize(ref retval, lastWritePos + 1);
            return retval;
        }

        static bool CompareSeq(IEnumerable<ulong> a, IEnumerable<ulong> b) {
            var aEnum = a.GetEnumerator();
            var bEnum = b.GetEnumerator();
            bool aHasNext = aEnum.MoveNext();
            bool bHasNext = bEnum.MoveNext();
            ulong aVal = aHasNext ? aEnum.Current : 0;
            ulong bVal = bHasNext ? bEnum.Current : 0;

            while (aHasNext && bHasNext) {
                if (aVal == bVal) {
                    aHasNext = aEnum.MoveNext();
                    aVal = aHasNext ? aEnum.Current : 0;
                    bHasNext = bEnum.MoveNext();
                    bVal = bHasNext ? bEnum.Current : 0;
                } else {
                    return false;
                }
            }
            if (bHasNext || aHasNext) {
                return false;
            } else {
                return true;
            }

        }

        static bool IsSorted(IEnumerable<ulong> a) {
            var aEnum = a.GetEnumerator();
            if(!aEnum.MoveNext())
                return true;
            var aVal = aEnum.Current;
            while (aEnum.MoveNext()) {
                var last = aVal;
                if ((aVal = aEnum.Current) < last)
                    return false;
            }
            return true;
        }


        static IEnumerable<ulong> MergeOrdered(IEnumerable<ulong> a, IEnumerable<ulong> b) {
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
        
        static void IntersectOrdered<T>(IEnumerable<T> a, IEnumerable<T> b, Action<T,T> foundPair)  where T:IComparable<T> { 
            var aEnum = a.GetEnumerator();
            var bEnum = b.GetEnumerator();
            bool aHasNext = aEnum.MoveNext();
            bool bHasNext = bEnum.MoveNext();
            T aVal=aHasNext? aEnum.Current:default(T);
            T bVal=bHasNext? bEnum.Current:default(T);

            while (aHasNext&&bHasNext) {
                int cmp = aVal.CompareTo(bVal);
                if (cmp<0) {
                    aHasNext = aEnum.MoveNext();
                    aVal = aHasNext ? aEnum.Current : default(T);
                } else if(cmp>0) {
                    bHasNext = bEnum.MoveNext();
                    bVal = bHasNext ? bEnum.Current : default(T);
                } else {
                    foundPair(aVal,bVal);
                    aHasNext = aEnum.MoveNext();
                    aVal = aHasNext ? aEnum.Current : default(T);
                    bHasNext = bEnum.MoveNext();
                    bVal = bHasNext ? bEnum.Current : default(T);
                }
            }
        }


        static LastFmTools tools;
        static void Main(string[] args) {
            SongDatabaseConfigFile config = new SongDatabaseConfigFile(false);
            tools = new LastFmTools(config);


            VerifyDataSetsAreSubsets();

            VerifyTriangleInequality();
        }

        static void VerifyTriangleInequality() {
            SimilarTracks sims = SimilarTracks.LoadOrCache(tools, LastFMspider.SimilarTracks.DataSetType.Complete);

            using (var writer = File.OpenWrite("errlog.log"))
            using (var swriter = new StreamWriter(writer)) {
                DateTime lastUpdate = DateTime.Now;
                int triangleCount = 0;
                int simCount = 0;
                int errCount = 0;
                double errSum = 0;
                double distSum = 0;
                float log100 = (float)Math.Log(100.0);
                Func<float, float> ratingToDist = rating => log100 - (float)Math.Log(rating);

                foreach (var similarity in sims.SimilaritiesRemapped) {
                    simCount++;
                    distSum += ratingToDist( similarity.Rating);
                    var simToA = sims.SimilarTo(similarity.TrackA);
                    var simToB = sims.SimilarTo(similarity.TrackB);
                    IntersectOrdered(simToA, simToB, (b, c) => {
                        triangleCount++;
                        float aD = ratingToDist(similarity.Rating);
                        float bD = ratingToDist(b.rating);
                        float cD = ratingToDist(c.rating);
                        float longest = Math.Max(aD, Math.Max(bD, cD));
                        float rest = aD + bD + cD - longest;
                        triangleCount++;
                        if (longest > rest) {
                            errCount++;
                            errSum += longest - rest;
                            swriter.WriteLine("{3}: {0},{1},{2}: {4}/{5}, avgDist:{6}",
                                similarity.TrackA, similarity.TrackB, b.trackID, longest - rest, errCount, triangleCount,distSum/simCount);
                        }
                    });
                    if (DateTime.Now - lastUpdate > TimeSpan.FromSeconds(1)) {
                        Console.WriteLine("Error rate: {0}/{1}, Progress:{2}", errCount, triangleCount, simCount / (double)sims.Count);
                        lastUpdate = DateTime.Now;
                    }


                }

            }
        }
        public static void VerifyDataSetsAreSubsets() {
            NiceTimer timer = new NiceTimer();
            Console.WriteLine("Verifying that all sets are subsets of DB.");

            timer.TimeMark("loading DB set");
            var fromDB = UniqueSimilarities((countPush, itemPush) => {
                tools.SimilarSongs.backingDB.RawSimilarTracks.Execute(true, countPush, itemPush);
            });
            Console.Write("FromDB.Count "); Console.WriteLine(fromDB.Count());
            Console.Write("IsSorted(FromDB)? "); Console.WriteLine(IsSorted(fromDB));

            timer.TimeMark("loading complete set");
            var completeSet = UniqueSimilarities(SimilarTracks.LoadOrCache(tools, SimilarTracks.DataSetType.Complete));

            timer.TimeMark("Comparing...");
            Console.Write("DB == Complete? "); Console.WriteLine(completeSet.SequenceEqual(fromDB));
            fromDB = null;


            timer.TimeMark("Loading Test");
            var testSet = UniqueSimilarities(SimilarTracks.LoadOrCache(tools, SimilarTracks.DataSetType.Test));

            timer.TimeMark("Loading Training");
            var trainSet = UniqueSimilarities(SimilarTracks.LoadOrCache(tools, SimilarTracks.DataSetType.Training));

            timer.TimeMark("Loading Verification...");
            var verSet = UniqueSimilarities(SimilarTracks.LoadOrCache(tools, SimilarTracks.DataSetType.Verification));

            timer.TimeMark("Comparing...");
            var mergeSet = MergeOrdered(testSet, MergeOrdered(verSet, trainSet));

            Console.Write("IsSorted(Complete)? "); Console.WriteLine(IsSorted(completeSet));
            Console.Write("IsSorted(Test)? "); Console.WriteLine(IsSorted(testSet));
            Console.Write("IsSorted(Train)? "); Console.WriteLine(IsSorted(trainSet));
            Console.Write("IsSorted(Ver)? "); Console.WriteLine(IsSorted(verSet));
            Console.Write("IsSorted(Merged)? "); Console.WriteLine(IsSorted(mergeSet));

            Console.Write("Complete.Count "); Console.WriteLine(completeSet.Count());
            Console.Write("Test.Count "); Console.WriteLine(testSet.Count());
            Console.Write("Train.Count "); Console.WriteLine(trainSet.Count());
            Console.Write("Ver.Count "); Console.WriteLine(verSet.Count());
            Console.Write("Merged.Count "); Console.WriteLine(mergeSet.Count());


            Console.Write("Complete = Test+Train+Verification? "); Console.WriteLine(CompareSeq(completeSet, mergeSet));


        }
    }
}
