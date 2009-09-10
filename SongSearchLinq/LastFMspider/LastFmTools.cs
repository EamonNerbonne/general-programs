using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using System.IO;
using EmnExtensions.Algorithms;
using EmnExtensions;
using LastFMspider.LastFMSQLiteBackend;
using LastFMspider.OldApi;
using System.Net;
using System.Threading;
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
			Parallel.ForEach(songsToDownload, songref => {
				//foreach (SongRef songref in songsToDownload) {
				try {
					lock (songsToDownload) progressCount++;
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
				} catch (Exception e) {
					Console.WriteLine("Exception: {0}", e.ToString());
				}//ignore all errors.
			});
			Console.WriteLine("Done precaching.");
		}

		public void EnsureLocalFilesInDB() {
			Console.WriteLine("Loading song database...");
			if (DB.InvalidDataCount != 0) Console.WriteLine("Ignored {0} songs with unknown tags (should be 0).", DB.InvalidDataCount);
			Console.WriteLine("Taking those {0} songs and indexing em by artist/title...", DB.Songs.Count);
			SongRef[] songsToDownload = Lookup.dataByRef.Keys.ToArray();
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



		public int PrecacheSongSimilarity() {
			int songsCached = 0;
			Console.WriteLine("Finding songs without similarities");
			var tracksToGo = SimilarSongs.backingDB.TracksWithoutSimilarityList.Execute(900000);
#if !DEBUG
			tracksToGo.Shuffle();
#endif
			tracksToGo = tracksToGo.Take(300000).ToArray();
			Console.WriteLine("Looking up similarities for {0} tracks...", tracksToGo.Length);
			Parallel.ForEach(tracksToGo, track => {
				StringBuilder msg = new StringBuilder();
				try {
					string trackStr = track.SongRef.ToString();
					msg.AppendFormat("SimTo:{0,-30}", trackStr.Substring(0, Math.Min(trackStr.Length, 30)));
					TrackSimilarityListInfo listStatus = SimilarSongs.backingDB.LookupSimilarityListAge.Execute(track.SongRef);
					if (listStatus.LookupTimestamp.HasValue) {
						msg.AppendFormat("done.");
					} else {
						var newEntry = OldApiClient.Track.GetSimilarTracks(track.SongRef);
						msg.AppendFormat("={0,3} ", newEntry.similartracks.Length);
						if (newEntry.similartracks.Length > 0)
							msg.AppendFormat("{1}: {0}", newEntry.similartracks[0].similarsong.ToString().Substring(0, Math.Min(newEntry.similartracks[0].similarsong.ToString().Length, 30)), newEntry.similartracks[0].similarity);

						SimilarSongs.backingDB.InsertSimilarityList.Execute(newEntry);
						lock (tracksToGo) songsCached++;
					}
				} catch (Exception e) {
					try {
						SimilarSongs.backingDB.InsertSimilarityList.Execute(SongSimilarityList.CreateErrorList(track.SongRef, 1));//unknown error => code 1
						lock (tracksToGo) songsCached++;
					} catch (Exception ee) { Console.WriteLine(ee.ToString()); }
					msg.AppendFormat("\n{0}: {1}\n", e.GetType().Name, e.Message);
				} finally {
					Console.WriteLine(msg);
				}
			});
			return songsCached;
		}


		public int PrecacheArtistSimilarity() {
			int artistsCached = 0;
			Console.WriteLine("Finding artists without similarities");
			var artistsToGo = SimilarSongs.backingDB.ArtistsWithoutSimilarityList.Execute(1000000);
#if !DEBUG
			artistsToGo.Shuffle();
#endif
			artistsToGo = artistsToGo.Take(100000).ToArray();
			Console.WriteLine("Looking up similarities for {0} artists...", artistsToGo.Length);
			Parallel.ForEach(artistsToGo, artist => {
				StringBuilder msg = new StringBuilder();
				try {
					msg.AppendFormat("SimTo:{0,-30}", artist.ArtistName.Substring(0, Math.Min(artist.ArtistName.Length, 30)));
					ArtistQueryInfo info = SimilarSongs.backingDB.LookupArtistSimilarityListAge.Execute(artist.ArtistName);
					if (info.LookupTimestamp.HasValue || info.IsAlternateOf.HasValue) {
						msg.AppendFormat("done.");
					} else {
						ArtistSimilarityList newEntry = OldApiClient.Artist.GetSimilarArtists(artist.ArtistName);
						msg.AppendFormat("={0,3} ", newEntry.Similar.Length);
						if (newEntry.Similar.Length > 0)
							msg.AppendFormat("{1}: {0}", newEntry.Similar[0].Artist.Substring(0, Math.Min(newEntry.Similar[0].Artist.Length, 30)), newEntry.Similar[0].Rating);

						if (artist.ArtistName.ToLatinLowercase() != newEntry.Artist.ToLatinLowercase())
							SimilarSongs.backingDB.SetArtistAlternate.Execute(artist.ArtistName, newEntry.Artist);
						SimilarSongs.backingDB.InsertArtistSimilarityList.Execute(newEntry);
						lock (artistsToGo) artistsCached++;
					}
				} catch (Exception e) {
					try {
						SimilarSongs.backingDB.InsertArtistSimilarityList.Execute(ArtistSimilarityList.CreateErrorList(artist.ArtistName, 1));
						lock (artistsToGo) artistsCached++;
					} catch (Exception ee) { Console.WriteLine(ee.ToString()); }
					msg.AppendFormat("\n{0}: {1}\n", e.GetType().Name, e.Message);
				} finally {
					Console.WriteLine(msg);
				}
			});
			return artistsCached;
		}


		public int PrecacheArtistTopTracks() {
			int artistsCached = 0;
			Console.WriteLine("Finding artists without toptracks");
			var artistsToGo = SimilarSongs.backingDB.ArtistsWithoutTopTracksList.Execute(1000000);
#if !DEBUG
			artistsToGo.Shuffle();
#endif
			artistsToGo = artistsToGo.Take(100000).ToArray();
			Console.WriteLine("Looking up top-tracks for {0} artists...", artistsToGo.Length);
			Parallel.ForEach(artistsToGo, artist => {
				StringBuilder msg = new StringBuilder();

				try {
					msg.AppendFormat("TopOf:{0,-30}", artist.ArtistName.Substring(0, Math.Min(artist.ArtistName.Length, 30)));
					ArtistQueryInfo info = SimilarSongs.backingDB.LookupArtistTopTracksListAge.Execute(artist.ArtistName);
					if (info.LookupTimestamp.HasValue || info.IsAlternateOf.HasValue) {
						msg.AppendFormat("done.");
					} else {

						var newEntry = OldApiClient.Artist.GetTopTracks(artist.ArtistName);
						msg.AppendFormat("={0,3} ", newEntry.TopTracks.Length);
						if (newEntry.TopTracks.Length > 0)
							msg.AppendFormat("{1}: {0}", newEntry.TopTracks[0].Track.Substring(0, Math.Min(newEntry.TopTracks[0].Track.Length, 30)), newEntry.TopTracks[0].Reach);

						if (artist.ArtistName.ToLatinLowercase() != newEntry.Artist.ToLatinLowercase())
							SimilarSongs.backingDB.SetArtistAlternate.Execute(artist.ArtistName, newEntry.Artist);

						SimilarSongs.backingDB.InsertArtistTopTracksList.Execute(newEntry);
						lock (artistsToGo) artistsCached++;
					}
				} catch (Exception e) {
					try {
						SimilarSongs.backingDB.InsertArtistTopTracksList.Execute(ArtistTopTracksList.CreateErrorList(artist.ArtistName, 1));
						lock (artistsToGo) artistsCached++;
					} catch (Exception ee) { Console.WriteLine(ee.ToString()); }
					msg.AppendFormat("\n{0}: {1}\n", e.GetType().Name, e.Message);
				} finally {
					Console.WriteLine(msg);
				}
			});
			return artistsCached;
		}

	}
}
