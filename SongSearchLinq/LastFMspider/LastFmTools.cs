using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using System.IO;
using EmnExtensions.Algorithms;
using LastFMspider.LastFMSQLiteBackend;
using LastFMspider.OldApi;
using System.Net;
namespace LastFMspider
{
    public class LastFmTools
    {
        SongSimilarityCache similarSongs;
        SongDatabaseConfigFile configFile;
        SimpleSongDB db;
        SongDataLookups lookup;
      //  private object sync = new object();

        public SongSimilarityCache SimilarSongs {
            get {
               // lock (sync)
                    return similarSongs ?? (similarSongs = new SongSimilarityCache(ConfigFile));
            }
        }
        public SongDatabaseConfigFile ConfigFile { get { return configFile; } }
        public SimpleSongDB DB {
            get {
               // lock (sync)
                    return db ?? (db = new SimpleSongDB(ConfigFile, null));
            }
        }
        public SongDataLookups Lookup {
            get {
              //  lock (sync)
                    return lookup ?? (lookup = new SongDataLookups(DB.Songs, null));
            }
        }

        public LastFmTools(SongDatabaseConfigFile configFile) {
            this.configFile = configFile;
        }


        public void UnloadLookup() {
           // lock (sync)
                lookup = null;
        }

        public void UnloadDB() {
          //  lock (sync) {
                UnloadLookup();
                db = null;
           // }
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
                    int newSimilars = similar == null || similar.similartracks == null ? 0 : similar.similartracks.Length;
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

        public void EnsureLocalFilesInDB() {
            Console.WriteLine("Loading song database...");
            if (DB.InvalidDataCount != 0) Console.WriteLine("Ignored {0} songs with unknown tags (should be 0).", DB.InvalidDataCount);
            Console.WriteLine("Taking those {0} songs and indexing em by artist/title...", DB.Songs.Count);
            SongRef[] songsToDownload = Lookup.dataByRef.Keys.ToArray();
            UnloadDB();
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


        private static IEnumerable<T> DeNull<T>(IEnumerable<T> iter) { return iter == null ? Enumerable.Empty<T>() : iter; }

        public void PrecacheSongSimilarity() {
            var tracksToGo = SimilarSongs.backingDB.TracksWithoutSimilarityList.Execute(1000000);
#if !DEBUG
            tracksToGo.Shuffle();
#endif
            tracksToGo = tracksToGo.Take(100000).ToArray();
            Console.WriteLine("Looking up similarities for {0} tracks...", tracksToGo.Length);
            foreach (var track in tracksToGo) {
                StringBuilder msg = new StringBuilder();
                try {
                    string trackStr = track.SongRef.ToString();
                    msg.AppendFormat("SimTo:{0,-30}", trackStr.Substring(0, Math.Min(trackStr.Length, 30)));
                    ListStatus? listStatus = SimilarSongs.backingDB.LookupSimilarityListAge.Execute(track.SongRef);
                    if (listStatus != null) {
                        msg.AppendFormat("done.");
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
                            similartracks = DeNull(simTracks.track).Select(simTrack => new SimilarTrack {
                                similarity = simTrack.match,
                                similarsong = SongRef.Create(simTrack.artist.name, simTrack.name),
                            }).ToArray(),
                        };
                    msg.AppendFormat("={0,3} ", newEntry.similartracks.Length);
                    if (newEntry.similartracks.Length > 0)
                        msg.AppendFormat("{1}: {0}", newEntry.similartracks[0].similarsong.ToString().Substring(0, Math.Min(newEntry.similartracks[0].similarsong.ToString().Length, 30)), newEntry.similartracks[0].similarity);

                    SimilarSongs.backingDB.InsertSimilarityList.Execute(newEntry);
                } catch (Exception e) {
                    msg.AppendFormat("{0}", e);
                } finally {
                    Console.WriteLine(msg);
                }
            }
        }


        public void PrecacheArtistSimilarity() {
            var artistsToGo = SimilarSongs.backingDB.ArtistsWithoutSimilarityList.Execute(1000000);
#if !DEBUG
            artistsToGo.Shuffle();
#endif
            Console.WriteLine("Looking up similarities for {0} artists...", artistsToGo.Length);
            foreach (var artist in artistsToGo.Take(10000)) {
                StringBuilder msg = new StringBuilder();
                try {
                    msg.AppendFormat("SimTo:{0,-30}", artist.ArtistName.Substring(0, Math.Min(artist.ArtistName.Length, 30)));
                    DateTime? previousAge = SimilarSongs.backingDB.LookupArtistSimilarityListAge.Execute(artist.ArtistName);
                    if (previousAge != null) {
                        msg.AppendFormat("done.");
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
                            Artist = artist.ArtistName.ToLatinLowercase() == simArtists.artistName.ToLatinLowercase() ?
                                simArtists.artistName : artist.ArtistName,
                            LookupTimestamp = DateTime.UtcNow,
                            Similar = DeNull(simArtists.artist).Select(simArtist => new SimilarArtist {
                                Artist = simArtist.name,
                                Rating = simArtist.match,
                            }).ToArray(),
                        };
                    msg.AppendFormat("={0,3} ", newEntry.Similar.Length);
                    if (newEntry.Similar.Length > 0)
                        msg.AppendFormat("{1}: {0}", newEntry.Similar[0].Artist.Substring(0, Math.Min(newEntry.Similar[0].Artist.Length, 30)), newEntry.Similar[0].Rating);

                    SimilarSongs.backingDB.InsertArtistSimilarityList.Execute(newEntry);
                } catch (WebException we) {
                    msg.AppendFormat("-Error:{0}", we.Status);
                    //SimilarSongs.backingDB.InsertArtistSimilarityList.Execute(newEntry);
                } catch (Exception e) {
                    msg.AppendFormat("{0}", e);
                } finally {
                    Console.WriteLine(msg);
                }
            }
        }


        public void PrecacheArtistTopTracks() {
            var artistsToGo = SimilarSongs.backingDB.ArtistsWithoutTopTracksList.Execute(1000000);
#if !DEBUG
            artistsToGo.Shuffle();
#endif
            Console.WriteLine("Looking up top-tracks for {0} artists...", artistsToGo.Length);

            foreach (var artist in artistsToGo) {
                StringBuilder msg = new StringBuilder();

                try {
                    msg.AppendFormat("TopOf:{0,-30}", artist.ArtistName.Substring(0, Math.Min(artist.ArtistName.Length, 30)));
                    DateTime? previousAge = SimilarSongs.backingDB.LookupArtistTopTracksListAge.Execute(artist.ArtistName);
                    if (previousAge != null) {
                        msg.AppendFormat("done.");
                        continue;
                    }
                    ApiArtistTopTracks artistTopTracks = OldApiClient.Artist.GetTopTracks(artist.ArtistName);
                    var newEntry = artistTopTracks == null
                        ? new ArtistTopTracksList {//represents 404 Not Found
                            Artist = artist.ArtistName,
                            LookupTimestamp = DateTime.UtcNow,
                            TopTracks = new ArtistTopTrack[0],
                        }
                        : new ArtistTopTracksList {
                            Artist = artistTopTracks.artist,
                            LookupTimestamp = DateTime.UtcNow,
                            TopTracks = DeNull(artistTopTracks.track).Select(toptrack => new ArtistTopTrack {
                                Track = toptrack.name,
                                Reach = toptrack.reach,
                            }).ToArray(),
                        };
                    msg.AppendFormat("={0,3} ", newEntry.TopTracks.Length);
                    if (newEntry.TopTracks.Length > 0)
                        msg.AppendFormat("{1}: {0}", newEntry.TopTracks[0].Track.Substring(0, Math.Min(newEntry.TopTracks[0].Track.Length, 30)), newEntry.TopTracks[0].Reach);


                    SimilarSongs.backingDB.InsertArtistTopTracksList.Execute(newEntry);
                } catch (Exception e) {
                    Console.WriteLine("{0}", e);
                } finally {
                    Console.WriteLine(msg);
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
