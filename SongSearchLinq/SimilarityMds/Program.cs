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
            SimilarityFormat format = SimilarityFormat.AvgRank;

            NiceTimer timer = new NiceTimer();
            timer.TimeMark("loading config");
            SongDatabaseConfigFile config = new SongDatabaseConfigFile(false);
            tools = new LastFmTools(config);
            timer.TimeMark("Loading training data");
            var sims = SimilarTracks.LoadOrCache(tools, SimilarTracks.DataSetType.Training);
            timer.TimeMark("Converting...");
            int soFar = 0;
            sims.ConvertDataFormat(SimilarityFormat.AvgRank, p => {
                if ((int)(p * 100) > soFar) {
                    soFar = (int)(p * 100);
                    Console.Write(soFar.ToString()+"% ");
                }
            });
            timer.TimeMark("GC");
            System.GC.Collect();
            timer.TimeMark("Idendifying already cached tracks");
            DirectoryInfo dataDir = tools.ConfigFile.DataDirectory;
            BitArray cachedDists = new BitArray(sims.TrackMapper.Count, false);
            bool atLeastOneTrackCached = false;
            foreach (int cachedTrack in CachedDistanceMatrix.AllTracksCached(dataDir,format).Select(nfile => nfile.number)) {
                cachedDists[cachedTrack] = true;
                atLeastOneTrackCached = true;
            }
            

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

            int cachedCount = Enumerable.Range(0, sims.TrackMapper.Count).Where(i => cachedDists[i]).Count();

            timer.TimeMark("Dijkstra's");
            Parallel.For(0, Enumerable.Range(0, sims.TrackMapper.Count).Where(i => !cachedDists[i]).Count(), (ignore) => {
                //                try {
                int track;
                float origTrackDist;
                int sequenceNumber;
                bool choiceWasRandom = false;
                lock (shortestDistanceToAny) {
                    if (cachedCount > 30 && r.Next(2) == 0) {
                        track = shortestDistanceToAny.IndexOfMax((candidate, dist) => !cachedDists[candidate] && dist.IsFinite());
                    } else {
                        do { track = r.Next(shortestDistanceToAny.Length); } while (cachedDists[track]);
                        choiceWasRandom = true;
                    }
                    cachedDists[track] = true;
                    sequenceNumber = cachedCount++;
                    origTrackDist = shortestDistanceToAny[track];
                }
                Console.WriteLine("Processing {0} {3} (dist = {1}, count = {2})", track, origTrackDist, sequenceNumber, choiceWasRandom ? "randomly" : "max-dist ");
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
                FileInfo saveFile = CachedDistanceMatrix.FileForTrack(dataDir,format, track);
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
                }
                Console.WriteLine("Done: {0}", track);
                //          } catch { }
            });
            timer.Done();
        }
    }
}
