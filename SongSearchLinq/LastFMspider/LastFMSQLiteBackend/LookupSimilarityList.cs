using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.SQLite;

namespace LastFMspider.LastFMSQLiteBackend {
	public class LookupSimilarityList : AbstractLfmCacheOperation {
		public LookupSimilarityList(LastFMSQLiteCache lfmCache) : base(lfmCache) { }
		public SongSimilarityList Execute(TrackSimilarityListInfo list) {
			if (!list.ListID.HasValue || list.SongRef == null) return null;

			lock (SyncRoot) {
				using (var trans = Connection.BeginTransaction()) {
					var similarto =
						from simTrack in list.SimilarTracks
						select new SimilarTrack {
							id = simTrack.OtherId,
							similarity = simTrack.Similarity,
							similarsong = lfmCache.LookupTrack.Execute(simTrack.OtherId)
						};

					return new SongSimilarityList {
						id = list.ListID,
						songref = list.SongRef,
						similartracks = similarto.ToArray(),
						LookupTimestamp = list.LookupTimestamp.Value,
						StatusCode = list.StatusCode,
					};
				}
			}
		}
	}
}
