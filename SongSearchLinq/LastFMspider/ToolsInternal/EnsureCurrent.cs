using System;
using System.Threading;
using LastFMspider.OldApi;

namespace LastFMspider
{
    internal static partial class ToolsInternal
    {

		public static Tuple<TrackSimilarityListInfo, SongSimilarityList> EnsureCurrent(LastFMSQLiteCache LastFmCache, SongRef songref, TimeSpan maxAge) {
			TrackSimilarityListInfo cachedVersion = LastFmCache.LookupSimilarityListInfo.Execute(songref);
			if (!cachedVersion.ListID.HasValue || !cachedVersion.LookupTimestamp.HasValue || cachedVersion.LookupTimestamp.Value < DateTime.UtcNow - maxAge) { //get online version
				Console.Write("?" + songref);
				var retval = OldApiClient.Track.GetSimilarTracks(songref);
				Console.WriteLine(" [" + retval.similartracks.Length + "]");
				try {
					return Tuple.Create(LastFmCache.InsertSimilarityList.Execute(retval), retval);
				} catch {//retry; might be a locking issue.  only retry once.
					Thread.Sleep(100);
					return Tuple.Create(LastFmCache.InsertSimilarityList.Execute(retval), retval);
				}
			} else
				return Tuple.Create(cachedVersion, default(SongSimilarityList));
		}
	}
}
