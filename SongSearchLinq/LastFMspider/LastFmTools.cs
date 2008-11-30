using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using System.IO;
using EmnExtensions.Algorithms;
using LastFMspider.LastFMSQLiteBackend;
using LastFMspider.OldApi;
namespace LastFMspider
{
    public class LastFmTools
    {
        SongSimilarityCache similarSongs;
        SongDatabaseConfigFile configFile;
        SimpleSongDB db;
        SongDataLookups lookup;

        public SongSimilarityCache SimilarSongs {
            get {
                return similarSongs ?? (similarSongs = new SongSimilarityCache(ConfigFile));
            }
        }
        public SongDatabaseConfigFile ConfigFile { get { return configFile; } }
        public SimpleSongDB DB {
            get {
                return db ?? (db = new SimpleSongDB(ConfigFile, null));
            }
        }
        public SongDataLookups Lookup {
            get {
                return lookup ?? (lookup = new SongDataLookups(DB.Songs, null));
            }
        }

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
            if (shuffle) songsToDownload.Shuffle();
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


        public void PrecacheSongSimilarityOld() { //TODO: redo this function like PrecacheArtistSimilarity for better robustness in the face of 404's and Url-encoding...            var stats = SimilarSongs.LookupDbStats();
            var stats = SimilarSongs.LookupDbStats();
            Console.WriteLine("Found {0} songs which don't have similarity stats.", stats.Length, stats.Where(s => s.LookupTimestamp != null).Count());
            stats.Shuffle(); //we shuffle the list so that parallel processes don't lookup the data in the same order.
            //Array.Sort(stats, (a, b) => b.TimesReferenced.CompareTo(a.TimesReferenced));
            Console.WriteLine("Showing a few...");

            var tolookup = new HashSet<SongRef>(stats.Select(stat => stat.SongRef));
            stats = null;

            while (tolookup.Count > 0) {
                var songref = tolookup.First();
                tolookup.Remove(songref);
                Console.WriteLine("{0}", songref.ToString());
                bool isNewlyDownloaded;
                try {
                    var simlist = SimilarSongs.Lookup(songref, out isNewlyDownloaded);
                    if (isNewlyDownloaded)
                        foreach (var sim in simlist.similartracks)
                            tolookup.Add(sim.similarsong);
                } catch (Exception e) {
                    try {
                        Console.WriteLine("Error in {0}: {1}: {2}", songref.ToString(), e.Message, e.StackTrace);
                    } catch { }
                }
                //shown++;

                //if (shown % 20 == 0) { Console.WriteLine("Press any key for more"); Console.ReadKey(); }
            }
        }
        private static IEnumerable<T> DeNull<T>(IEnumerable<T> iter) { return iter == null ? Enumerable.Empty<T>() : iter; }

        public void PrecacheSongSimilarity() {
            var tracksToGo = SimilarSongs.backingDB.TracksWithoutSimilarityList.Execute(1000000);
#if !DEBUG
            tracksToGo.Shuffle();
#endif
            Console.WriteLine("Looking up similarities for {0} tracks...", tracksToGo.Length);
            foreach (var track in tracksToGo) {
                try {
                    string trackStr = track.SongRef.ToString();
                    Console.Write("SimTo:{0,-30}", trackStr.Substring(0, Math.Min(trackStr.Length, 30)));
                    DateTime? previousAge = SimilarSongs.backingDB.LookupSimilarityListAge.Execute(track.SongRef);
                    if (previousAge != null) {
                        Console.WriteLine("done.");
                        continue;
                    }

                    ApiTrackSimilarTracks simTracks = OldApiClient.Track.GetSimilarTracks(track.SongRef);
                    var newEntry = simTracks == null
                        ? new SongSimilarityList {//represents 404 Not Found
                            LookupTimestamp = DateTime.UtcNow,
                             songref = track.SongRef,
                              similartracks = new SimilarTrack[0],
                        }
                        : new SongSimilarityList {
                            LookupTimestamp = DateTime.UtcNow,
                            songref = track.SongRef,
                            similartracks= DeNull(simTracks.track).Select(simTrack=>new SimilarTrack{
                                 similarity = simTrack.match,
                                  similarsong = SongRef.Create(simTrack.artist.name,simTrack.name),
                            }).ToArray(),
                        };
                    Console.Write("={0,3} ", newEntry.similartracks.Length);
                    if (newEntry.similartracks.Length > 0)
                        Console.Write("{1}: {0}", newEntry.similartracks[0].similarsong.ToString().Substring(0, Math.Min(newEntry.similartracks[0].similarsong.ToString().Length, 30)), newEntry.similartracks[0].similarity);

                    SimilarSongs.backingDB.InsertSimilarityList.Execute(newEntry);
                    Console.WriteLine(".");
                } catch (Exception e) {
                    Console.WriteLine("{0}", e);
                }
            }
        }


        public void PrecacheArtistSimilarity() {
            var artistsToGo = SimilarSongs.backingDB.ArtistsWithoutSimilarityList.Execute(-1);
#if !DEBUG
            artistsToGo.Shuffle();
#endif
            Console.WriteLine("Looking up similarities for {0} artists...", artistsToGo.Length);
            foreach (var artist in artistsToGo) {
                try {
                    Console.Write("SimTo:{0,-30}", artist.ArtistName.Substring(0,Math.Min(artist.ArtistName.Length,30)));
                    DateTime? previousAge = SimilarSongs.backingDB.LookupArtistSimilarityListAge.Execute(artist.ArtistName);
                    if (previousAge != null) {
                        Console.WriteLine("done.");
                        continue;
                    }

                    ApiArtistSimilarArtists simArtists = OldApiClient.Artist.GetSimilarArtists(artist.ArtistName);
                    var newEntry = simArtists == null
                        ? new ArtistSimilarityList {//represents 404 Not Found
                            Artist = artist.ArtistName,
                            LookupTimestamp = DateTime.UtcNow,
                            Similar = new SimilarArtist[] { }
                        }
                        : new ArtistSimilarityList {
                            Artist = simArtists.artistName,
                            LookupTimestamp = DateTime.UtcNow,
                            Similar = DeNull(simArtists.artist).Select(simArtist => new SimilarArtist {
                                Artist = simArtist.name,
                                Rating = simArtist.match,
                            }).ToArray(),
                        };
                    Console.Write("={0,3} ", newEntry.Similar.Length);
                    if(newEntry.Similar.Length>0)
                        Console.Write("{1}: {0}", newEntry.Similar[0].Artist.Substring(0, Math.Min(newEntry.Similar[0].Artist.Length, 30)), newEntry.Similar[0].Rating);

                    SimilarSongs.backingDB.InsertArtistSimilarityList.Execute(newEntry);
                    Console.WriteLine(".");
                } catch (Exception e) {
                    Console.WriteLine("{0}", e);
                }
            }
        }


        public void PrecacheArtistTopTracks() {
            var artistsToGo = SimilarSongs.backingDB.ArtistsWithoutTopTracksList.Execute(100);
#if !DEBUG
            artistsToGo.Shuffle();
#endif
            Console.WriteLine("Looking up top-tracks for {0} artists...", artistsToGo.Length);

            foreach (var artist in artistsToGo) {
                try {
                    Console.Write("TopOf:{0,-30}", artist.ArtistName.Substring(0, Math.Min(artist.ArtistName.Length, 30)));
                    DateTime? previousAge = SimilarSongs.backingDB.LookupArtistTopTracksListAge.Execute(artist.ArtistName);
                    if (previousAge != null) {
                        Console.WriteLine("done.");
                        continue;
                    }
                    ApiArtistTopTracks artistTopTracks = OldApiClient.Artist.GetTopTracks(artist.ArtistName);
                    var newEntry= artistTopTracks == null
                        ?new ArtistTopTracksList {//represents 404 Not Found
                            Artist = artist.ArtistName,
                            LookupTimestamp = DateTime.UtcNow,
                            TopTracks =  new ArtistTopTrack[0],
                        }
                        :new ArtistTopTracksList {
                            Artist = artistTopTracks.artist,
                            LookupTimestamp = DateTime.UtcNow,
                            TopTracks = DeNull(artistTopTracks.track).Select(toptrack => new ArtistTopTrack {
                                Track = toptrack.name,
                                Reach = toptrack.reach,
                            }).ToArray(),
                        };
                    Console.Write("={0,3} ", newEntry.TopTracks.Length);
                    if (newEntry.TopTracks.Length > 0)
                        Console.Write("{1}: {0}", newEntry.TopTracks[0].Track.Substring(0, Math.Min(newEntry.TopTracks[0].Track.Length, 30)), newEntry.TopTracks[0].Reach);


                    SimilarSongs.backingDB.InsertArtistTopTracksList.Execute(newEntry);
                } catch (Exception e) {
                    Console.WriteLine("{0}", e);
                }
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
