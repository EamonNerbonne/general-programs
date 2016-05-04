using System.Data.Common;
using SongDataLib;

namespace LastFMspider.LastFMSQLiteBackend {
	public class LookupSimilarityListInfo : AbstractLfmCacheQuery {
		public LookupSimilarityListInfo(LastFMSQLiteCache lfmCache)
			: base(lfmCache) {
			trackId = DefineParameter("@trackId");
		}
		protected override string CommandText {
			get {
				return @"
                    SELECT T.CurrentSimilarTrackListTimestamp, T.CurrentSimilarTrackList
                    FROM Track T
                    WHERE T.TrackID=@trackId
                ";
			}
		}

		readonly DbParameter trackId;

		public TrackSimilarityListInfo Execute(SongRef songref) {
			return DoInLockedTransaction(() => ExecuteImpl(lfmCache.LookupTrackID.Execute(songref)));
		}
		public TrackSimilarityListInfo Execute(TrackId id) {
			if (!id.HasValue) return TrackSimilarityListInfo.CreateUnknown(id);
			return DoInLockedTransaction(() => ExecuteImpl(id));
		}
		TrackSimilarityListInfo ExecuteImpl(TrackId id) {
			trackId.Value = id.Id;
			var vals = CommandObj.ExecuteGetTopRow();
			//we expect exactly one hit - or none
			if (vals == null) return TrackSimilarityListInfo.CreateUnknown(id);
			return new TrackSimilarityListInfo(
				trackId: id,
				lookupTimestamp: vals[0].CastDbObjectAsDateTime(),
				statusCode: 0,
				similarTracks: new SimilarityList<TrackId, TrackId.Factory>(vals[1].CastDbObjectAs<byte[]>()));
		}
	}
}
