using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using SongDataLib;

namespace LastFMspider.LastFMSQLiteBackend {
	public class InsertArtistTopTracksList : AbstractLfmCacheQuery {
		public InsertArtistTopTracksList(LastFMSQLiteCache lfm)
			: base(lfm) {
			artistID = DefineParameter("@artistID");
			lookupTimestamp = DefineParameter("@lookupTimestamp");
			statusCode = DefineParameter("@statusCode");
			listBlob = DefineParameter("@listBlob", DbType.Binary);
		}
		readonly DbParameter artistID, lookupTimestamp, statusCode, listBlob;
		protected override string CommandText {
			get {
				return @"
INSERT INTO [TopTracksList] (ArtistID, LookupTimestamp,StatusCode,TopTracks) 
VALUES (@artistID, @lookupTimestamp, @statusCode, @listBlob);

SELECT L.ListID
FROM TopTracksList L
WHERE L.ArtistID = @artistID
AND L.LookupTimestamp = @lookupTimestamp
LIMIT 1
";
			}
		}

		public ArtistTopTracksListInfo Execute(ArtistTopTracksList toptracksList) {
			return DoInLockedTransaction(() => {
				ArtistId baseId = lfmCache.InsertArtist.Execute(toptracksList.Artist);
				var listImpl = new ReachList<TrackId, TrackId.Factory>(
						from tt in toptracksList.TopTracks
						select
							new HasReach<TrackId>(
								lfmCache.UpdateTrackCasing.Execute(
									SongRef.Create(toptracksList.Artist, tt.Track)
								), tt.Reach
							)
					);
				artistID.Value = baseId.Id;
				lookupTimestamp.Value = toptracksList.LookupTimestamp.ToUniversalTime().Ticks;
				statusCode.Value = toptracksList.StatusCode;
				listBlob.Value = listImpl.encodedSims;
				TopTracksListId listId = new TopTracksListId(CommandObj.ExecuteScalar().CastDbObjectAs<long>());

				if (toptracksList.LookupTimestamp.ToUniversalTime() > DateTime.UtcNow - TimeSpan.FromDays(1.0))
					lfmCache.ArtistSetCurrentTopTracks.Execute(listId); //presume if this is recently downloaded, then it's the most current.

				return new ArtistTopTracksListInfo(listId, new ArtistInfo { ArtistId = baseId, Artist = toptracksList.Artist }, toptracksList.LookupTimestamp.ToUniversalTime(), toptracksList.StatusCode, listImpl);
			});
		}


	}
}
