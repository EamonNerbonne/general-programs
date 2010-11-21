using System.Linq;

namespace LastFMspider.LastFMSQLiteBackend {
	public class LookupSimilarityList : AbstractLfmCacheOperation {
		public LookupSimilarityList(LastFMSQLiteCache lfmCache) : base(lfmCache) { }
		public SongSimilarityList Execute(TrackSimilarityListInfo list) {
			if (!list.ListID.HasValue) return null;

			return DoInLockedTransaction(() => {
				var similarto =
									from simTrack in list.SimilarTracks
									let similarsong = lfmCache.LookupTrack.Execute(simTrack.OtherId)
									where similarsong != null
									select new SimilarTrack {
										id = simTrack.OtherId,
										similarity = simTrack.Similarity,
										similarsong = similarsong
									};

				return new SongSimilarityList {
					id = list.ListID,
					songref = lfmCache.LookupTrack.Execute(list.TrackId),
					similartracks = similarto.ToArray(),
					LookupTimestamp = list.LookupTimestamp.Value.ToUniversalTime(),
					StatusCode = list.StatusCode,
				};
			});
		}
	}
}
