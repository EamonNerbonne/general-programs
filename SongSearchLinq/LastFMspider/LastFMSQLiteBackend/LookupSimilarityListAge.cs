using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public class LookupSimilarityListAge : AbstractLfmCacheQuery {
		public LookupSimilarityListAge(LastFMSQLiteCache lfmCache)
			: base(lfmCache) {
			trackId = DefineParameter("@trackId");
		}
		protected override string CommandText {
			get {
				return @"
SELECT L.ListID, L.LookupTimestamp, L.StatusCode, L.SimilarTracks
FROM Track T, SimilarTrackList L
WHERE T.TrackID=@trackId
AND L.ListID = T.CurrentSimilarTrackList
";
			}
		}
		DbParameter trackId;

		public TrackSimilarityListInfo Execute(SongRef songref) {
			return DoInLockedTransaction(() => ExecuteImpl(songref, lfmCache.LookupTrackID.Execute(songref)));
		}
		public TrackSimilarityListInfo Execute(TrackId id) {
			if (!id.HasValue) return TrackSimilarityListInfo.CreateUnknown(null, id);
			return DoInLockedTransaction(() => ExecuteImpl(lfmCache.LookupTrack.Execute(id), id));
		}
		public TrackSimilarityListInfo Execute(SongRef songref, TrackId id) {
			if (!id.HasValue) return TrackSimilarityListInfo.CreateUnknown(songref, id);
			return DoInLockedTransaction(() => ExecuteImpl(songref, id));
		}
		TrackSimilarityListInfo ExecuteImpl(SongRef songref, TrackId id) {
			trackId.Value = id.Id;
			var vals = CommandObj.ExecuteGetTopRow();
			//we expect exactly one hit - or none
			if (vals == null) return TrackSimilarityListInfo.CreateUnknown(songref, id);
			return new TrackSimilarityListInfo(
				listID: new SimilarTracksListId((long)vals[0]),
				trackId: id,
				songref: songref,
				lookupTimestamp: vals[1].CastDbObjectAsDateTime().Value,
				statusCode: (int?)vals[2].CastDbObjectAs<long?>(),
				sims: vals[3].CastDbObjectAs<byte[]>());
		}

	}
}
