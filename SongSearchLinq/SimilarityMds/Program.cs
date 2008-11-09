using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using LastFMspider;
using EmnExtensions;
using EmnExtensions.Algorithms;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;

namespace SimilarityMds
{
    class Program
    {
        static LastFmTools tools;

        static Dijkstra.DistanceTo ConvertStruct(SimilarTracks.DenseSimilarTo sim) {
            return new Dijkstra.DistanceTo {
                targetNode = sim.trackID,
                distance = sim.rating
            };
        }

        static void Main(string[] args) {
            SongDatabaseConfigFile config = new SongDatabaseConfigFile(false);
            tools = new LastFmTools(config);
            SimCacheManager settings = new SimCacheManager(SimilarityFormat.LastFmRating, tools, DataSetType.Training);

            var allformats = new[] { SimilarityFormat.AvgRank, SimilarityFormat.AvgRank2, SimilarityFormat.Log200, SimilarityFormat.Log2000 };
            foreach (var format in allformats ) {
                Precache(settings.WithFormat(format) , 13000);
            }
            foreach (var format in allformats) {
                CachedDistanceMatrix cachedMatrix = settings.WithFormat(format).LoadCachedDistanceMatrix();
                int nextPercent = 1;
                cachedMatrix.LoadDistFromAllCacheFiles(d => { 
                    if(d*100>=nextPercent){
                        Console.Write("{0}% ", nextPercent);
                        nextPercent++;
                    }
                }, true);
            }
        }
        static void Precache(SimCacheManager settings,int maxToCache){

            NiceTimer timer = new NiceTimer();
            timer.TimeMark("Idendifying already cached tracks");
            var cachedTrackNumbers=settings.AllTracksCached.Select(nfile => nfile.number).ToArray();
            if (cachedTrackNumbers.Length >= maxToCache) {
                Console.WriteLine("Sufficient tracks ({0}) cached, {1} requested.", cachedTrackNumbers.Length, maxToCache);
                timer.Done();
                return;
            } else {
                Console.WriteLine("Requested {0} tracks, finished {1}", maxToCache, cachedTrackNumbers.Length);
            }
            timer.TimeMark("Loading training data");
            var sims = settings.LoadSimilarTracks();
            timer.TimeMark("GC");
            System.GC.Collect();
            timer.TimeMark("Marking cached tracks");
            BitArray cachedDists = new BitArray(sims.TrackMapper.Count, false);
            bool atLeastOneTrackCached = false;
            foreach (int cachedTrack in cachedTrackNumbers) {
                cachedDists[cachedTrack] = true;
                atLeastOneTrackCached = true;
            }
            int cachedCount = cachedTrackNumbers.Length;
            cachedTrackNumbers = null;


            float[] shortestDistanceToAny;
            int[] shortedPathToAny;
            Random r = new Random();
            if (atLeastOneTrackCached) {
                timer.TimeMark("Finding distance to any cached track");

                Dijkstra.FindShortestPath(
                    (numNode) => sims.SimilarTo(numNode).Select(
                        sim => new Dijkstra.DistanceTo {
                            targetNode = sim.trackID,
                            distance = sim.rating
                        }),
                    sims.TrackMapper.Count,
                    Enumerable.Range(0, sims.TrackMapper.Count).Where(i => cachedDists[i]),
                    out shortestDistanceToAny,
                    out shortedPathToAny);
            }  else {
                timer.TimeMark("Initializing distances to inf");

                shortestDistanceToAny = Enumerable.Repeat(float.PositiveInfinity, sims.TrackMapper.Count).ToArray();
            }


            timer.TimeMark("Dijkstra's");
            Parallel.For(0, maxToCache-cachedCount, (ignore) => {
                int track;
                float origTrackDist;
                int sequenceNumber;
                bool choiceWasRandom = false;
                lock (shortestDistanceToAny) {
                    if (cachedCount > 0 && r.Next(2) == 0) {
                        track = shortestDistanceToAny.IndexOfMax((candidate, dist) => !cachedDists[candidate] && dist.IsFinite());
                    } else {
                        do { track = r.Next(shortestDistanceToAny.Length); } while (cachedDists[track]);
                        choiceWasRandom = true;
                    }
                    cachedDists[track] = true;
                    sequenceNumber = cachedCount;
                    origTrackDist = shortestDistanceToAny[track];
                }
                Console.WriteLine("Processing {0} {3} (dist = {1}, count = {2}/{4})", track, origTrackDist, sequenceNumber, choiceWasRandom ? "randomly" : "max-dist ",maxToCache);
                float[] distanceFromA;
                int[] pathToA;

                Dijkstra.FindShortestPath(
                    (numNode) => sims.SimilarTo(numNode).Select(
                        sim => new Dijkstra.DistanceTo {
                            targetNode = sim.trackID,
                            distance = sim.rating
                        }),
                    sims.TrackMapper.Count,
                    Enumerable.Repeat(track, 1),
                    out distanceFromA,
                    out  pathToA);
                FileInfo saveFile = settings.DijkstraFileOfTrackNumber( track);
                using (Stream s = saveFile.Open(FileMode.Create, FileAccess.Write))
                using (var binW = new BinaryWriter(s)) {
                    binW.Write(distanceFromA.Length);
                    foreach (var f in distanceFromA)
                        binW.Write(f);
                }
                lock (shortestDistanceToAny) {
                    for (int i = 0; i < shortestDistanceToAny.Length; i++) {
                        shortestDistanceToAny[i] = Math.Min(shortestDistanceToAny[i], distanceFromA[i]);
                    }
                    cachedCount++;
                }
                Console.WriteLine("Done: {0}", track);
            });
            timer.Done();
        }
    }
}
