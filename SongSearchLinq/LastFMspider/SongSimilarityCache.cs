using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using LastFMspider.LastFMSQLiteBackend;
using System.Xml;

using EmnExtensions.Text;
using EmnExtensions.Web;
using EmnExtensions.Collections;
using EmnExtensions;
using System.Diagnostics;
using SongDataLib;
using LastFMspider.OldApi;
using System.Threading;

namespace LastFMspider {
	public class SongSimilarityCache {
		public LastFMSQLiteCache backingDB { get; private set; }

		public SongSimilarityCache(SongDatabaseConfigFile configFile) {
			Init(configFile);
		}


		private void Init() {
			Console.WriteLine("Loading config...");
			var configFile = new SongDatabaseConfigFile(false);
			Init(configFile);
		}

		private void Init(SongDatabaseConfigFile configFile) {
			Console.WriteLine("Initializing sqlite db");
			backingDB = new LastFMSQLiteCache(configFile);
		}

		static readonly TimeSpan normalMaxAge = TimeSpan.FromDays(365.0);

		public SongSimilarityList LookupMaybe(SongRef songref, TimeSpan maxAge = default(TimeSpan)) {
			if (maxAge == default(TimeSpan)) maxAge = normalMaxAge;
			var songSimListAge = backingDB.LookupSimilarityListInfo.Execute(songref);
			if (!IsFresh(songSimListAge, maxAge))
				QueueWebRequest(songref);

			return backingDB.LookupSimilarityList.Execute(songSimListAge);
		}

		private void QueueWebRequest(SongRef songref) {
			lock (syncTodoReq) {
				todoReq.Add(songref);
				if (todoReq.Count == 1) //I'm the first, start processing!
					ThreadPool.QueueUserWorkItem(ProcessQueuedLookups);
			}
		}
		void ProcessQueuedLookups(object ignore) {
			while (true) {
				SongRef next;
				lock (syncTodoReq)
					next = todoReq.FirstOrDefault();
				var simInfo = backingDB.LookupSimilarityListInfo.Execute(next);
				if (!IsFresh(simInfo, TimeSpan.FromDays(2.0)))
					DoWebLookup(backingDB, next);
				lock (syncTodoReq) {
					todoReq.Remove(next);
					if (todoReq.Count == 0)
						break;
				}
			}
		}

		public SongSimilarityList Lookup(SongRef songref, TimeSpan maxAge = default(TimeSpan)) {
			if (maxAge == default(TimeSpan)) maxAge = normalMaxAge;
			return Lookup(backingDB.LookupSimilarityListInfo.Execute(songref), maxAge);
		}

		static object syncSongSimLookup = new object();
		class Syncer {
			public int InUse = 0;
			public SongSimilarityList retval;
			public bool IsInUse { get { return InUse > 0; } }
			public void Claim() { lock (this) InUse++; }
			public void Release() { lock (this) InUse--; }
		}
		static Dictionary<SongRef, Syncer> inFlight = new Dictionary<SongRef, Syncer>();
		object syncTodoReq = new object();
		static HashSet<SongRef> todoReq = new HashSet<SongRef>();

		static SongSimilarityList DoWebLookup(LastFMSQLiteCache backingDB, SongRef songref) {
			Syncer sync = null;
			lock (syncSongSimLookup) {
				if (!inFlight.TryGetValue(songref, out sync))
					inFlight[songref] = sync = new Syncer();
			}
			try {
				sync.Claim();
				Console.Write("?" + songref);
				lock (sync) {
					if (sync.retval == null) {
						sync.retval = OldApiClient.Track.GetSimilarTracks(songref);
						Console.WriteLine(" [" + sync.retval.similartracks.Length + "]");
						try {
							backingDB.InsertSimilarityList.Execute(sync.retval);
						} catch {//retry; might be a locking issue.  only retry once.
							System.Threading.Thread.Sleep(100);
							backingDB.InsertSimilarityList.Execute(sync.retval);
						}
					}
					return sync.retval;
				}
			} finally {
				sync.Release();
				lock (syncSongSimLookup)
					if (!sync.IsInUse)
						inFlight.Remove(songref);//OK to repeat
			}
		}

		static bool IsFresh(TrackSimilarityListInfo cachedVersion, TimeSpan maxAge) {
			return cachedVersion.ListID.HasValue && cachedVersion.LookupTimestamp.HasValue && cachedVersion.LookupTimestamp.Value >= DateTime.UtcNow - maxAge;
		}

		public SongSimilarityList Lookup(TrackSimilarityListInfo cachedVersion, TimeSpan maxAge) {
			if (!IsFresh(cachedVersion, maxAge))  //get online version
				return DoWebLookup(backingDB, backingDB.LookupTrack.Execute(cachedVersion.TrackId));
			else
				return backingDB.LookupSimilarityList.Execute(cachedVersion);
		}

		public Tuple<TrackSimilarityListInfo, SongSimilarityList> EnsureCurrent(SongRef songref, TimeSpan maxAge) {
			TrackSimilarityListInfo cachedVersion = backingDB.LookupSimilarityListInfo.Execute(songref);
			if (!cachedVersion.ListID.HasValue || !cachedVersion.LookupTimestamp.HasValue || cachedVersion.LookupTimestamp.Value < DateTime.UtcNow - maxAge) { //get online version
				Console.Write("?" + songref);
				var retval = OldApiClient.Track.GetSimilarTracks(songref);
				Console.WriteLine(" [" + retval.similartracks.Length + "]");
				try {
					return Tuple.Create(backingDB.InsertSimilarityList.Execute(retval), retval);
				} catch {//retry; might be a locking issue.  only retry once.
					System.Threading.Thread.Sleep(100);
					return Tuple.Create(backingDB.InsertSimilarityList.Execute(retval), retval);
				}
			} else
				return Tuple.Create(cachedVersion, default(SongSimilarityList));
		}


		public ArtistTopTracksList LookupTopTracks(string artist, TimeSpan maxAge = default(TimeSpan)) {
			//artist = artist.ToLatinLowercase();
			if (maxAge == default(TimeSpan)) maxAge = normalMaxAge;
			var toptracksInfo = backingDB.LookupArtistTopTracksListAge.Execute(artist);
			if (toptracksInfo.IsKnown && toptracksInfo.LookupTimestamp.HasValue && toptracksInfo.LookupTimestamp.Value >= DateTime.UtcNow - maxAge)
				return backingDB.LookupArtistTopTracksList.Execute(toptracksInfo);
			if (toptracksInfo.ArtistInfo.IsAlternateOf.HasValue)
				return LookupTopTracks(backingDB.LookupArtist.Execute(toptracksInfo.ArtistInfo.IsAlternateOf), maxAge);

			ArtistTopTracksList toptracks;
			try {
				toptracks = OldApiClient.Artist.GetTopTracks(artist);
			} catch (Exception) {
				toptracks = ArtistTopTracksList.CreateErrorList(artist, 1);//TODO:statuscodes...
			}
			if (artist.ToLatinLowercase() != toptracks.Artist.ToLatinLowercase())
				backingDB.SetArtistAlternate.Execute(artist, toptracks.Artist);
			backingDB.InsertArtistTopTracksList.Execute(toptracks);
			return toptracks;
		}

		public ArtistSimilarityList LookupSimilaArtists(string artist, TimeSpan maxAge = default(TimeSpan)) {
			if (maxAge == default(TimeSpan)) maxAge = normalMaxAge;
			var simartistInfo = backingDB.LookupArtistSimilarityListAge.Execute(artist);
			if (simartistInfo.ListID.HasValue && simartistInfo.LookupTimestamp.HasValue && simartistInfo.LookupTimestamp.Value >= DateTime.UtcNow - maxAge)
				return backingDB.LookupArtistSimilarityList.Execute(simartistInfo);
			if (simartistInfo.ArtistInfo.IsAlternateOf.HasValue)
				return LookupSimilaArtists(backingDB.LookupArtist.Execute(simartistInfo.ArtistInfo.IsAlternateOf), maxAge);

			ArtistSimilarityList simartists;
			try {
				simartists = OldApiClient.Artist.GetSimilarArtists(artist);
			} catch (Exception) {
				simartists = ArtistSimilarityList.CreateErrorList(artist, 1);//TODO:statuscodes...
			}
			if (artist.ToLatinLowercase() != simartists.Artist.ToLatinLowercase())
				backingDB.SetArtistAlternate.Execute(artist, simartists.Artist);
			backingDB.InsertArtistSimilarityList.Execute(simartists);
			return simartists;

		}
	}
}
