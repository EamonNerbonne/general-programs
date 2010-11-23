using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public class LookupSimilarityListInfo : AbstractLfmCacheQuery {
		public LookupSimilarityListInfo(LastFMSQLiteCache lfmCache)
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

		readonly DbParameter trackId;

		public TrackSimilarityListInfo Execute(SongRef songref) {
			return DoInLockedTransaction(() => ExecuteImpl(lfmCache.LookupTrackID.Execute(songref)));
		}
		public TrackSimilarityListInfo Execute(TrackId id) {
			if (!id.HasValue) return TrackSimilarityListInfo.CreateUnknown(id);
			return  DoInLockedTransaction(() => ExecuteImpl(id));
		}
		TrackSimilarityListInfo ExecuteImpl(TrackId id) {
			trackId.Value = id.Id;
			var vals = CommandObj.ExecuteGetTopRow();
			//we expect exactly one hit - or none
			if (vals == null) return TrackSimilarityListInfo.CreateUnknown(id);
			return new TrackSimilarityListInfo(
				listID: new SimilarTracksListId((long)vals[0]),
				trackId: id,
				lookupTimestamp: vals[1].CastDbObjectAsDateTime().Value,
				statusCode: (int?)vals[2].CastDbObjectAs<long?>(),
				similarTracks: new SimilarityList<TrackId, TrackId.Factory>(vals[3].CastDbObjectAs<byte[]>()));
		}
	}
}
