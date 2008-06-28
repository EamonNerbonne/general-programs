using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LastFMspider;
using SongDataLib;

namespace LastFmCacheExtender
{
    class Program
    {
        static void Main(string[] args) {
            var config = new SongDatabaseConfigFile(false);
            Console.WriteLine("Loading song similarity...");
            //var similarSongs = new SongSimilarityCache(config);
            var tools = new LastFmTools(config);
            tools.PrecacheAudioscrobbler();
            tools.PrecacheSongSimilarity();
        }
    }
}
