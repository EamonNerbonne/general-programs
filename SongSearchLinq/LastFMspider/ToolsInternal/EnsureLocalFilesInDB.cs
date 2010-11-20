using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmnExtensions.Algorithms;
using LastFMspider.LastFMSQLiteBackend;
using LastFMspider.OldApi;

namespace LastFMspider
{
    internal static partial class ToolsInternal
    {

        public static void EnsureLocalFilesInDB(LastFmTools tools)
        {
            var DB = tools.SongsOnDisk;
            var SimilarSongs = tools.SimilarSongs;

            Console.WriteLine("Loading song database...");
            if (DB.InvalidDataCount != 0)
                Console.WriteLine("Ignored {0} songs with unknown tags (should be 0).", DB.InvalidDataCount);
            Console.WriteLine("Taking those {0} songs and indexing em by artist/title...", DB.Songs.Length);
            SongRef[] songsToDownload = tools.FindByName.Select(group=>group.Key).ToArray();
            lock (SimilarSongs.backingDB.SyncRoot)
                using (var trans = SimilarSongs.backingDB.Connection.BeginTransaction()) {
                    foreach (SongRef songref in songsToDownload) {
                        try {
                            SimilarSongs.backingDB.InsertTrack.Execute(songref);
                        } catch (Exception e) {
                            Console.WriteLine("Exception: {0}", e.ToString());
                        }//ignore all errors.
                    }
                    trans.Commit();
                }
        }
    }
}
