using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using LastFMspider;
using LastFMspider.LastFMSQLiteBackend;
using EamonExtensionsLinq;
using System.IO;

namespace SimilarityAnalysis
{

    class DistanceMem
    {
        public List<SortedList<int, float>> distances = new List<SortedList<int, float>>();

        public void AddDistance(int a, int b, float dist) {
            distances[a][b] = dist;
            distances[b][a] = dist;
        }


        public void RegTrack(int trackNum) {
            while (distances.Count <= trackNum) {
                distances.Add(new SortedList<int, float>());
            }
        }
    }
    class TrackMap
    {
        static void Noop(int ignore) { }
        public Action<int> AddedTrack = new Action<int>(Noop);
        Dictionary<int, int> oldToNew = new Dictionary<int, int>();
        int nextSongIndex = 0;
        public int Map(int oldIndex) {
            int newIndex;
            if (!oldToNew.TryGetValue(oldIndex, out newIndex)) {
                oldToNew[oldIndex] = newIndex = nextSongIndex;
                nextSongIndex++;
                AddedTrack(newIndex);
            }
            return newIndex;
        }
    }


    class Program
    {

        //sparseness
        static void Main(string[] args) {
            NiceTimer timer = new NiceTimer(null);
            var config = new SongDatabaseConfigFile(false);

            timer.TimeMark("Loading song similarity...");
            var tools = new LastFmTools(config);
            tools.UseSimilarSongs();
            TrackMap mapper = new TrackMap();
            DistanceMem dmem = new DistanceMem();
            mapper.AddedTrack = dmem.RegTrack;
            double offset = Math.Log(-100.0);
            foreach (var relation in tools.SimilarSongs.backingDB.RawSimilarTracks.Execute(true)) {
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
        }
    }
}
