using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using LastFMspider;
using EmnExtensions;
using EmnExtensions.Algorithms;

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
            timer.TimeMark("Dijkstra");
            Random r = new Random();

            int trackA = r.Next(sims.TrackMapper.Count);
            float[] distanceFromA;
            int[] pathToA;

            Dijkstra.FindShortestPath(
                (numNode) => sims.SimilarTo(numNode).Select(
                    sim => new Dijkstra.DistanceTo {
                        targetNode = sim.trackID,
                        distance = sim.rating
                    }),
                sims.TrackMapper.Count,
                trackA,
                out distanceFromA,
                out  pathToA);
            timer.TimeMark("Reporting");

            int trackB = distanceFromA.IndexOfMax(dist=>dist!=float.PositiveInfinity);
            Console.WriteLine("Going From {0} to {1} in {2}:",trackA,trackB,distanceFromA[trackB]);
            int curr = trackB;
            while (curr != trackA) {
                Console.WriteLine("{0} dist at {1}, going {2} via {3}", distanceFromA[curr], curr, sims.FindRating( pathToA[curr],curr), pathToA[curr]);
                curr = pathToA[curr];
            }


            timer.Done();
        }
    }
}
