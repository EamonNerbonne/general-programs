using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

		readonly BlockingCollection<TrackSimilarityListInfo> BgLookupQueue= new BlockingCollection<TrackSimilarityListInfo>();
		int procthreads;
		void ProcessQueue() {
			int currActive = Interlocked.Increment(ref procthreads);
			try {
				if (currActive == 1)
					foreach (var list in BgLookupQueue.GetConsumingEnumerable())
						LookupSimilarTracksHelper.RefreshCache(tools.LastFmCache, list);
			} finally {
				Interlocked.Decrement(ref procthreads);
			}
		}

		internal void RefreshCacheIfNeeded(IEnumerable<Task<TrackSimilarityListInfo>> listtasks) {
			if(procthreads==0)
				new Thread(ProcessQueue) { IsBackground = true,}.Start();
			
			foreach(var listtask in listtasks)
				listtask.ContinueWith(task=>BgLookupQueue.Add( task.Result));
		}
	}
}
