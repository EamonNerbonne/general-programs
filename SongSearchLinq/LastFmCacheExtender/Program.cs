using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LastFMspider;
using SongDataLib;
using System.Threading;

namespace LastFmCacheExtender
{
    class Program
    {
        static LastFmTools tools;
        static void Main(string[] args) {
            
            var config = new SongDatabaseConfigFile(false);
            Console.WriteLine("Loading song similarity...");
            tools = new LastFmTools(config);
            tools.EnsureLocalFilesInDB();
            tools.UnloadDB();
            Console.WriteLine("go!");
            ExtendSimilarities();
        }

        static void ExtendSimilarities() {
            bool run = true;
            int errCount = 0;
            while (run&&errCount<10) {
                try {
                    run = false;
                    //tools.PrecacheLocalFiles(false);//might want to do this first, but meh.
                    run = 0 < tools.PrecacheArtistSimilarity()||run;
                    run = 0 < tools.PrecacheArtistTopTracks()||run;
                    run = 0 < tools.PrecacheSongSimilarity()||run;
                } catch {
                    run = true;
                    errCount++;
                }
            }
        }
    }
}
