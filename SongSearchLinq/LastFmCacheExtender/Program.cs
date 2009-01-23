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
            //tools.PrecacheLocalFiles(false);//might want to do this first, but meh.
            tools.PrecacheArtistSimilarity();
            tools.PrecacheArtistTopTracks();
            tools.PrecacheSongSimilarity();
        }
    }
}
