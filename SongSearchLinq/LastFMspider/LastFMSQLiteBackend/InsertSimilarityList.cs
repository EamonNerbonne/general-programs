using System;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace LastFMspider.LastFMSQLiteBackend {
	public class InsertSimilarityList : AbstractLfmCacheQuery {
		public InsertSimilarityList(LastFMSQLiteCache lfm)
			: base(lfm) {
			trackID = DefineParameter("@trackID");
			lookupTimestamp = DefineParameter("@lookupTimestamp");
			statusCode = DefineParameter("@statusCode");
			listBlob = DefineParameter("@listBlob", DbType.Binary);
		}
		readonly DbParameter trackID, lookupTimestamp, statusCode, listBlob;
		protected override string CommandText {
			get {
				return @"
INSERT INTO [SimilarTrackList] (TrackID, LookupTimestamp,StatusCode,SimilarTracks) 
VALUES (@trackID, @lookupTimestamp, @statusCode, @listBlob);

SELECT L.ListID
FROM SimilarTrackList L
WHERE L.TrackID = @trackID
AND L.LookupTimestamp = @lookupTimestamp
LIMIT 1
";
			}
		}

		public TrackSimilarityListInfo Execute(SongSimilarityList simList) {
			return DoInLockedTransaction(() => {
				TrackId baseTrackId = lfmCache.InsertTrack.Execute(simList.songref);
				SimilarityList<TrackId, TrackId.Factory> listImpl = new SimilarityList<TrackId, TrackId.Factory>(
						from simtrack in simList.similartracks
						select new SimilarityTo<TrackId>(lfmCache.UpdateTrackCasing.Execute(simtrack.similarsong), (float)simtrack.similarity)
					);

				trackID.Value = baseTrackId.Id;
				lookupTimestamp.Value = simList.LookupTimestamp.ToUniversalTime().Ticks;
				statusCode.Value = simList.StatusCode;
				listBlob.Value = listImpl.encodedSims;
				SimilarTracksListId listId = new SimilarTracksListId(CommandObj.ExecuteScalar().CastDbObjectAs<long>());

				if (simList.LookupTimestamp.ToUniversalTime() > DateTime.UtcNow - TimeSpan.FromDays(1.0))
					lfmCache.TrackSetCurrentSimList.Execute(listId); //presume if this is recently downloaded, then it's the most current.


				return new TrackSimilarityListInfo(listId, baseTrackId, simList.LookupTimestamp.ToUniversalTime(), simList.StatusCode, listImpl);
			});
		}
	}
}
