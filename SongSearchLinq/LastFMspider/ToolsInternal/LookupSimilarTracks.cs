
using System;
using SongDataLib;

namespace LastFMspider {
	public static class LookupSimilarTracksHelper {
		static readonly TimeSpan normalMaxAge = TimeSpan.FromDays(90.0);

		public static SongSimilarityList Lookup(SongTools tools, SongRef songref, TimeSpan maxAge = default(TimeSpan)) {
			return Lookup(tools, songref, tools.LastFmCache.LookupSimilarityListInfo.Execute(songref), maxAge);
		}

		public static SongSimilarityList Lookup(SongTools tools, SongRef songref, TrackSimilarityListInfo cachedVersion, TimeSpan maxAge = default(TimeSpan)) {
			return tools.LastFmCache.LookupSimilarityList.Execute(cachedVersion);
		}
	}
}
