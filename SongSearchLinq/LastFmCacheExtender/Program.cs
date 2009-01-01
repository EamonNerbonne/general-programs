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
            //var similarSongs = new SongSimilarityCache(config);
            tools = new LastFmTools(config);
            tools.EnsureLocalFilesInDB();
            tools.UnloadDB();
            object a, b, c, d, e, f, g,h;
            Console.WriteLine("go!");
#if DEBUG
            ExtendSimilarities();
#else

            Parallel.Invoke(Enumerable.Repeat((Action)ExtendSimilarities, 4).ToArray());
#endif
        }

        static void ExtendSimilarities() {
        //    tools.PrecacheArtistTopTracks();
            tools.PrecacheSongSimilarity();
            //tools.PrecacheSongSimilarity();
            //tools.PrecacheLocalFiles(false);//might want to do this first, but meh.
            //do {
            //    tools.PrecacheArtistSimilarity();
            //    tools.PrecacheArtistTopTracks();
            //} while (tools.SimilarSongs.backingDB.ArtistsWithoutSimilarityList.Execute(1).Length > 0);
            //tools.PrecacheSongSimilarity();
        }
    }
}
