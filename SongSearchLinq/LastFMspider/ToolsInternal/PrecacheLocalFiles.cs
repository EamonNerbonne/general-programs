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

        public static void PrecacheLocalFiles(LastFmTools tools, bool shuffle)
        {
            var DB = tools.DB;
            var SimilarSongs = tools.SimilarSongs;
            var Lookup = tools.Lookup;

            Console.WriteLine("Loading song database...");
            if (DB.InvalidDataCount != 0)
                Console.WriteLine("Ignored {0} songs with unknown tags (should be 0).", DB.InvalidDataCount);
            Console.WriteLine("Taking those {0} songs and indexing em by artist/title...", DB.Songs.Count);
            SongRef[] songsToDownload = Lookup.dataByRef.Keys.ToArray();
            if (shuffle)
                songsToDownload.Shuffle();
            tools.UnloadDB();
            Console.WriteLine("Downloading Last.fm similar tracks...");
            int progressCount = 0;
            int total = songsToDownload.Length;
            long similarityCount = 0;
            int hits = 0;
            Parallel.ForEach(songsToDownload, new ParallelOptions { MaxDegreeOfParallelism = 10 }, songref => {
                //foreach (SongRef songref in songsToDownload) {
                //try {
                lock (songsToDownload) {
                    progressCount++;
                    //if (100 * (progressCount - 1) / (double)total < 3.0 && 100 * progressCount / (double)total >= 3.0) {
                    //    Console.WriteLine("3%!");
                    //    Console.ReadKey();
                    //}
                }
                var similar = SimilarSongs.Lookup(songref, TimeSpan.FromDays(100.0));//precache the last.fm data.  unsure - NOT REALLY necessary?
                lock (songsToDownload) {
                    similarityCount += similar.similartracks.Length;
                    if (similar != null)
                        hits++;
                    Console.WriteLine("{0,3} - tot={4} in hits={5}, with relTo={3} in \"{1} - {2}\"",
                        100 * progressCount / (double)total,
                        songref.Artist,
                        songref.Title,
                        similar.similartracks.Length,
                        (double)similarityCount,
                        hits);
                }
                //} catch (Exception e) {
                //    Console.WriteLine("Exception: {0}", e.ToString());
                //}//ignore all errors.
            });
            Console.WriteLine("Done precaching.");
        }
    }
}
