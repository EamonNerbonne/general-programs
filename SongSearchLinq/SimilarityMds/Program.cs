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

            Random r = new Random(37);
            HashSet<int> selectedTracks = new HashSet<int>();
            while (selectedTracks.Count < 3000) {
                selectedTracks.Add(r.Next(sims.TrackMapper.Count));
            }
            var selTrackArr = selectedTracks.ToArray();
            File.WriteAllText(@"C:\emn\selected.tracks", string.Join("\n", selectedTracks.Select(t => t.ToString()).ToArray()));

            timer.TimeMark("Dijkstra");
            Parallel.ForEach(selTrackArr, (track) => {
                float[] distanceFromA;
                int[] pathToA;

                Dijkstra.FindShortestPath(
                    (numNode) => sims.SimilarTo(numNode).Select(
                        sim => new Dijkstra.DistanceTo {
                            targetNode = sim.trackID,
                            distance = sim.rating
                        }),
                    sims.TrackMapper.Count,
                    Enumerable.Repeat( track,1),
                    out distanceFromA,
                    out  pathToA);
                File.WriteAllText(@"C:\emn\t" + track + ".dist",
                    string.Join("",selTrackArr.Select(trackOther =>
                        "" + trackOther + " " + distanceFromA[trackOther] + "\n").ToArray()));
            });

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
            List<int> tracksTotal = new List<int>(selTrackArr);

            Parallel.For(0,tracksToGo.Count, (ignore) => {
                int track;
                lock (tracksToGo) {
                    track = tracksToGo.Dequeue();
                    tracksTotal.Add(track);
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
                string[] toWrite;
                lock(tracksToGo)
                    toWrite= tracksTotal.Select(trackOther =>
                        "" + trackOther + " " + distanceFromA[trackOther] + "\n").ToArray();
                File.WriteAllText(@"C:\emn\e" + track + ".dist",
                    string.Join("", toWrite) );
            });


            timer.Done();
        }
    }
}
