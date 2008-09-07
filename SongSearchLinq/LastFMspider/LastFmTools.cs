using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using System.IO;
using EamonExtensionsLinq.Algorithms;
namespace LastFMspider
{
    public class LastFmTools
    {
        public SongSimilarityCache SimilarSongs {get; protected set;}
        public SongDatabaseConfigFile ConfigFile {get;protected set;}
        public SimpleSongDB DB{get;protected set;}
        public SongDataLookups Lookup { get; protected set; }

        public LastFmTools(SongDatabaseConfigFile configFile) {
            ConfigFile = configFile;
        }

        public void UseDB() {
            if (DB == null) 
                DB= new SimpleSongDB(ConfigFile, null);
            
        }
        public void UseSimilarSongs() {
            if (SimilarSongs == null) 
            SimilarSongs = new SongSimilarityCache(ConfigFile);
        }
        public void UseLookup() {
            if (Lookup == null) {
                UseDB();
                Lookup=new SongDataLookups(DB.Songs, null);
            }
        }

        public void UnloadLookup() {
            Lookup = null;
        }

        public void UnloadDB() {
            UnloadLookup();
            DB = null;
        }

        public void PrecacheLocalFiles() {
            UseSimilarSongs();
            UseDB();
            UseLookup();
            Console.WriteLine("Loading song database...");
            if (DB.InvalidDataCount != 0) Console.WriteLine("Ignored {0} songs with unknown tags (should be 0).", DB.InvalidDataCount);
            Console.WriteLine("Taking those {0} songs and indexing em by artist/title...", DB.Songs.Count);
            SongRef[] songsToDownload = Lookup.dataByRef.Keys.ToArray();
            songsToDownload.Shuffle(); //linearspeed: comment this line out if not executing in parallel for a DB speed boost
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
            UseSimilarSongs();
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



    }
}
