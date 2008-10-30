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



    class Program
    {


        //sparseness
        static void Main(string[] args) {
            SongDatabaseConfigFile config = new SongDatabaseConfigFile(false);
            LastFmTools tools = new LastFmTools(config);
            SimilarTracks sims = SimilarTracks.LoadOrCache(tools, false);
            Console.WriteLine("Sum is {0}.", sims.Similarities.Select(row => (double)row.Rating).Sum());

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
