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
/*            object tmp1=0, tmp2=1;
            var dbcomm=tools.SimilarSongs.backingDB.Connection.CreateCommand();
            var dbcomm2 = tools.SimilarSongs.backingDB.Connection.CreateCommand();
            Console.WriteLine("Deleting similar tracks...");
            dbcomm.CommandText = @"
DELETE FROM SimilarTrack where TrackA in
(select T.TrackID from  Artist A, Track T where (A.lowercaseArtist like '%.' OR T.LowercaseTitle LIKE '%.') AND T.ArtistID = A.ArtistID )
";
            dbcomm2.CommandText = @"
UPDATE Track SET LookupTimestamp = NULL WHERE TrackID in
(select T.TrackID from  Artist A, Track T where (A.lowercaseArtist like '%.' OR T.LowercaseTitle LIKE '%.') AND T.ArtistID = A.ArtistID )
";
            Console.WriteLine("made commands, executing...");
            using (var trans = tools.SimilarSongs.backingDB.Connection.BeginTransaction()) {
                Console.WriteLine("Deleting similar tracks...");
                dbcomm.ExecuteNonQuery();
                Console.WriteLine("Setting timestamp to null...");
                dbcomm2.ExecuteNonQuery();
                Console.WriteLine("Committing");
                trans.Commit();
            }
            Console.WriteLine("done.");
            // */
            //tools.PrecacheSongSimilarity();
            tools.PrecacheArtistSimilarity();
            tools.PrecacheArtistTopTracks();
            tools.PrecacheLocalFiles(false);//might want to do this first, but meh.
            tools.PrecacheSongSimilarity();
        }
    }
}
