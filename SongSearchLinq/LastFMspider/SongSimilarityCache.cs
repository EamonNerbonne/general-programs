using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Threading;
using LastFMspider.LastFMSQLiteBackend;

namespace LastFMspider {
	public class SongSimilarityCache {
		readonly SongTools tools;
		public SongSimilarityCache(SongTools tools) { this.tools = tools; }

		/// <summary>
		/// Downloads Last.fm metadata for all tracks in the song database (if not already present).
		/// </summary>
		/// <param name="shuffle">Whether to perform the precaching in a random order.  Doing so slows down the precaching when almost all
		/// items are already downloaded, but permits multiple download threads to run in parallel without duplicating downloads.</param>
		public void PrecacheLocalFiles(bool shuffle = false) { ToolsInternal.PrecacheLocalFiles(tools, shuffle); }

		public void EnsureLocalFilesInDB() { ToolsInternal.EnsureLocalFilesInDB(tools); }

		public int PrecacheSongSimilarity() { return ToolsInternal.PrecacheSongSimilarity(tools); }

		public int PrecacheArtistSimilarity() { return ToolsInternal.PrecacheArtistSimilarity(tools); }

		public int PrecacheArtistTopTracks() { return ToolsInternal.PrecacheArtistTopTracks(tools); }
		public ArtistTopTracksList LookupTopTracks(string artist, TimeSpan fromDays = default(TimeSpan)) { return ToolsInternal.LookupTopTracks(tools.LastFmCache, artist, fromDays); }

		public ArtistSimilarityList LookupSimilarArtists(string artist, TimeSpan fromDays = default(TimeSpan)) { return ToolsInternal.LookupSimilarArtists(tools.LastFmCache, artist, fromDays); }


		readonly ConcurrentBag<TrackId[]> BgLookupQueue = new ConcurrentBag<TrackId[]>();
		bool isActive;
		readonly object sync = new object();
		public static int ErrorCount;

		void ProcessQueue() {
			TrackId[] tracksToUpdate;
			while (BgLookupQueue.TryTake(out tracksToUpdate))
				foreach (var track in tracksToUpdate)
					try {
						LookupSimilarTracksHelper.RefreshCache(tools.LastFmCache, track);
					} catch (SQLiteException) {
						Interlocked.Increment(ref ErrorCount);
					}
			lock (sync)
				isActive = false;
			StartQueue();
		}

		void StartQueue() {
			bool willStart = false;
			lock (sync)
				if (!isActive && (BgLookupQueue.Count > 0))
					willStart = isActive = true;

			if (willStart)
				new Thread(ProcessQueue).Start();
		}

		internal void RefreshCacheIfNeeded(TrackId[] listtasks) {
			BgLookupQueue.Add(listtasks);
			StartQueue();
		}
		internal void RefreshCacheIfNeeded(TrackSimilarityListInfo[] listtasks) {
			BgLookupQueue.Add(listtasks.Where(simListInfo => !LookupSimilarTracksHelper.IsFresh(simListInfo)).Select(simListInfo => simListInfo.TrackId).ToArray());
			StartQueue();
		}
	}
}
