using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LastFMspider.EDM;
using LastFMspider;
using SongDataLib;
using EamonExtensionsLinq.DebugTools;
namespace EdmTest
{
    class Program
    {
        static void Main(string[] args) {
            var config = new SongDatabaseConfigFile(false);
            Console.WriteLine("Loading song similarity...");
            //var similarSongs = new SongSimilarityCache(config);
            var tools = new LastFmTools(config);
            tools.UseSimilarSongs();
            var edm=tools.SimilarSongs.backingDB.EDMCont;
            (from track in edm.TrackSet
             let artist = track.Artist
             where track.LowercaseTitle.StartsWith("border")
             select new { Artist=artist.FullArtist, Title=track.FullTitle }).Take(30).PrintAllDebug();
            Console.WriteLine("done!");

            Console.ReadKey();
        }
    }
}
