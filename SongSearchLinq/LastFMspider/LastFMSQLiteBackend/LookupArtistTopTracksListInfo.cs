﻿using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public class LookupArtistTopTracksListInfo : AbstractLfmCacheQuery {
		public LookupArtistTopTracksListInfo(LastFMSQLiteCache lfmCache)
			: base(lfmCache) {
			artistId = DefineParameter("@artistId");
		}
		protected override string CommandText {
			get {
				return @"
SELECT L.ListID, L.ArtistId, L.LookupTimestamp, L.StatusCode, L.TopTracks
FROM Artist A, TopTracksList L
WHERE A.ArtistID = @artistId
AND  L.ListID = A.CurrentTopTracksList
";
			}
		}
		readonly DbParameter artistId;

		public ArtistTopTracksListInfo Execute(string artist) {
			return DoInLockedTransaction(() => {
				var artistInfo = lfmCache.LookupArtistInfo.Execute(artist);
				if (artistInfo.IsAlternateOf.HasValue)
					return ArtistTopTracksListInfo.CreateUnknown(artistInfo);
				else {
					artistId.Value = artistInfo.ArtistId.Id;

					using (var reader = CommandObj.ExecuteReader()) {
						//we expect exactly one hit - or none
						if (reader.Read())
							return new ArtistTopTracksListInfo(
								listID: new TopTracksListId((long)reader[0]),
								artistInfo: artistInfo,
								lookupTimestamp: reader[2].CastDbObjectAsDateTime().Value,
								statusCode: (int?)reader[3].CastDbObjectAs<long?>(),
								rankings: new ReachList<TrackId, TrackId.Factory>(reader[4].CastDbObjectAs<byte[]>() ?? new byte[] { })
								);
						else
							return ArtistTopTracksListInfo.CreateUnknown(artistInfo);
					}
				}
			});
		}
	}
}
