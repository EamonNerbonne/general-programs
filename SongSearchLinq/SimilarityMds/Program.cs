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
        static Regex fileNameRegex = new Regex(@"(e|t|b)(?<num>\d+)\.(dist|bin)", RegexOptions.CultureInvariant| RegexOptions.Compiled| RegexOptions.ExplicitCapture);

        static void Main(string[] args) {
          /*  Heap<int> test = new Heap<int>((i, j) => { });
            var nums = Enumerable.Range(1,100).ToArray();
            nums.Shuffle();
            foreach (int number in nums)
                test.Add(number);
            int sum=0;
            int currN;
            while (test.RemoveTop(out currN))
                sum += currN;
            Console.WriteLine("sum: "+sum);*/
            NiceTimer timer = new NiceTimer();
            timer.TimeMark("loading config");
            SongDatabaseConfigFile config = new SongDatabaseConfigFile(false);
            tools = new LastFmTools(config);
            timer.TimeMark("Loading training data");
            var sims = SimilarTracks.LoadOrCache(tools, SimilarTracks.DataSetType.Training);
            timer.TimeMark("Converting...");
            sims.ConvertToDistanceFormat();
            timer.TimeMark("GC");
            System.GC.Collect();

            DirectoryInfo dir = new DirectoryInfo(@"C:\emn\");
            HashSet<int> cachedDists = new HashSet<int>();
            foreach (var file in dir.GetFiles()) {
                if (fileNameRegex.IsMatch(file.Name)) {
                    int trackNum = int.Parse (fileNameRegex.Replace(file.Name, "${num}"));
                    cachedDists.Add(trackNum);
                }
            }

            Random r = new Random();

            timer.TimeMark("Dijkstra");
            Parallel.For(0,sims.TrackMapper.Count-cachedDists.Count, (i) => {
                try {
                    int track;
                    lock (r) {
                        do{track= r.Next(sims.TrackMapper.Count);}
                        while (cachedDists.Contains(track));
                        cachedDists.Add(track);
                    }
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
                    FileInfo saveFile = new FileInfo(@"C:\emn\b"+track+".bin");
                    using(Stream s = saveFile.Open(FileMode.Create, FileAccess.Write))
                    using (var binW = new BinaryWriter(s)) {
                        binW.Write(distanceFromA.Length);
                        foreach (var f in distanceFromA)
                            binW.Write(f);
                    }
                } catch { }
            });
            /*
            float[] distanceFromAll;
            int[] pathToAll;

            Dijkstra.FindShortestPath(
                (numNode) => sims.SimilarTo(numNode).Select(
                    sim => new Dijkstra.DistanceTo {
                        targetNode = sim.trackID,
                        distance = sim.rating
                    }),
                sims.TrackMapper.Count,
                selTrackArr,
                out distanceFromAll,
                out  pathToAll);

            Queue<int> tracksToGo = new Queue<int>(
            distanceFromAll
                .Select((d, i) => new { dist = d, track = i })
                .Where(d => d.dist != float.PositiveInfinity)
                .Where(d=>d.dist>0)
                .OrderByDescending(d => d.dist)
                .Select(d=>d.track)
                );
             */
            timer.Done();
        }
    }
}
