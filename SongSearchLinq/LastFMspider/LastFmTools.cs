using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using System.IO;
using EmnExtensions.Algorithms;
using LastFMspider.LastFMSQLiteBackend;
namespace LastFMspider
{
    public class LastFmTools
    {
        SongSimilarityCache similarSongs;
        SongDatabaseConfigFile configFile;
        SimpleSongDB db;
        SongDataLookups lookup;

        public SongSimilarityCache SimilarSongs { get {
                return similarSongs ?? (similarSongs = new SongSimilarityCache(ConfigFile));
        } }
        public SongDatabaseConfigFile ConfigFile { get { return configFile; } }
        public SimpleSongDB DB { get {
                return db ?? (db = new SimpleSongDB(ConfigFile, null));
            }
        }
        public SongDataLookups Lookup { get { 
            return lookup??(lookup = new SongDataLookups(DB.Songs, null)); 
        } }

        public LastFmTools(SongDatabaseConfigFile configFile) {
            this.configFile = configFile;
        }


        public void UnloadLookup() {
            lookup = null;
        }

        public void UnloadDB() {
            UnloadLookup();
            db = null;
        }

        public void PrecacheSongMetadata() {
            Console.WriteLine("Loading song database...");
            if (DB.InvalidDataCount != 0)
                Console.WriteLine("Ignored {0} songs with unknown tags (should be 0).", DB.InvalidDataCount);
            Console.WriteLine("Taking those {0} songs and indexing em by artist/title...", DB.Songs.Count);
            SongRef[] songsToDownload = Lookup.dataByRef.Keys.ToArray();
            songsToDownload.Shuffle(); //linearspeed: comment this line out if not executing in parallel for a DB speed boost
            UnloadDB();
            System.GC.Collect();
            Console.WriteLine("Downloading extra metadata from Last.fm...");
            int progressCount = 0;
            int total = songsToDownload.Length;
            foreach (SongRef songref in songsToDownload) {
                try {
                    progressCount++;
                } catch (Exception e) {
                    Console.WriteLine("Exception: {0}", e.ToString());
                }
            }
        }

        /// <summary>
        /// Downloads Last.fm metadata for all tracks in the song database (if not already present).
        /// </summary>
        /// <param name="shuffle">Whether to perform the precaching in a random order.  Doing so slows down the precaching when almost all
        /// items are already downloaded, but permits multiple download threads to run in parallel without duplicating downloads.</param>
        public void PrecacheLocalFiles(bool shuffle) {
            Console.WriteLine("Loading song database...");
            if (DB.InvalidDataCount != 0) Console.WriteLine("Ignored {0} songs with unknown tags (should be 0).", DB.InvalidDataCount);
            Console.WriteLine("Taking those {0} songs and indexing em by artist/title...", DB.Songs.Count);
            SongRef[] songsToDownload = Lookup.dataByRef.Keys.ToArray();
            if(shuffle) songsToDownload.Shuffle();
            UnloadDB();
            System.GC.Collect();
            Console.WriteLine("Downloading Last.fm similar tracks...");
            int progressCount = 0;
            int total = songsToDownload.Length;
            long similarityCount = 0;
            int hits = 0;
            foreach (SongRef songref in songsToDownload) {
                try {
                    progressCount++;
                    var similar = SimilarSongs.Lookup(songref);//precache the last.fm data.  unsure - NOT REALLY necessary?
                    int newSimilars = similar.similartracks == null ? 0 : similar.similartracks.Length;
                    similarityCount += newSimilars;
                    if (similar != null)
                        hits++;
                    Console.WriteLine("{0,3} - tot={4} in hits={5}, with relTo={3} in \"{1} - {2}\"",
                        100 * progressCount / (double)total,
                        songref.Artist,
                        songref.Title,
                        newSimilars,
                        (double)similarityCount,
                        hits);

                } catch (Exception e) {
                    Console.WriteLine("Exception: {0}", e.ToString());
                }//ignore all errors.
            }
            Console.WriteLine("Done precaching.");
        }


        public void PrecacheSongSimilarity() {
            var stats = SimilarSongs.LookupDbStats();

            Console.WriteLine("Found {0} songs which don't have similarity stats.", stats.Length, stats.Where(s => s.LookupTimestamp != null).Count());
            stats.Shuffle(); //we shuffle the list so that parallel processes don't lookup the data in the same order.
            //Array.Sort(stats, (a, b) => b.TimesReferenced.CompareTo(a.TimesReferenced));
            Console.WriteLine("Showing a few...");

            foreach (var stat in stats) {
                Console.WriteLine("{1} {0}, {2}", stat.SongRef.ToString(), stat.LookupTimestamp == null ? "!" : " ", stat.TimesReferenced);
                try { SimilarSongs.Lookup(stat.SongRef); } catch (Exception e) {
                    try {
                        Console.WriteLine("Error in {0}: {1}: {2}", stat.SongRef.ToString(), e.Message, e.StackTrace);
                    } catch { }
                }
                //shown++;

                //if (shown % 20 == 0) { Console.WriteLine("Press any key for more"); Console.ReadKey(); }
            }


        }
        public void PrecacheSongTags() {
            Console.WriteLine("Loading songs...");
            CachedTrack[] tracks = SimilarSongs.backingDB.AllTracks.Execute();
            Console.WriteLine("Found {0} tracks", tracks.Length);

            /*Console.WriteLine("Downloading extra metadata from Last.fm...");
            int progressCount = 0;
            int total = songsToDownload.Length;
            foreach (SongRef songref in songsToDownload) {
                try {
                    progressCount++;
                } catch (Exception e) {
                    Console.WriteLine("Exception: {0}", e.ToString());
                }
            }*/
        }




    }
}
