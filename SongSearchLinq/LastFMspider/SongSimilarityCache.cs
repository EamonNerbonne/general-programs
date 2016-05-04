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

		public void EnsureLocalFilesInDB() { ToolsInternal.EnsureLocalFilesInDB(tools); }

		public int PrecacheArtistSimilarity() { return ToolsInternal._PrecacheArtistSimilarity(tools); }

		public int PrecacheArtistTopTracks() { return ToolsInternal._PrecacheArtistTopTracks(tools); }
		public ArtistTopTracksList LookupTopTracks(string artist, TimeSpan fromDays = default(TimeSpan)) { return ToolsInternal.LookupTopTracks(tools.LastFmCache, artist, fromDays); }

		public ArtistSimilarityList LookupSimilarArtists(string artist, TimeSpan fromDays = default(TimeSpan)) { return ToolsInternal.LookupSimilarArtists(tools.LastFmCache, artist, fromDays); }
	}
}
