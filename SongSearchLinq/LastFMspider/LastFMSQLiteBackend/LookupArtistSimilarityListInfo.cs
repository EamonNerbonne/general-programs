using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public class LookupArtistSimilarityListInfo : AbstractLfmCacheQuery {
		public LookupArtistSimilarityListInfo(LastFMSQLiteCache lfmCache)
			: base(lfmCache) {
			artistId = DefineParameter("@artistId");
		}
		protected override string CommandText {
			get {
				return @"
SELECT L.ListID, L.ArtistId, L.LookupTimestamp, L.StatusCode, L.SimilarArtists
FROM Artist A, SimilarArtistList L
WHERE A.ArtistID = @artistId
AND  L.ListID = A.CurrentSimilarArtistList
";
			}
		}
		DbParameter artistId;

		public ArtistSimilarityListInfo Execute(string artist) {
			lock (SyncRoot) {
				//TODO:IsAlternateOf
				using (var trans = Connection.BeginTransaction()) {
					ArtistSimilarityListInfo retval;
					var artistInfo = lfmCache.LookupArtistInfo.Execute(artist);
					if (artistInfo.IsAlternateOf.HasValue)
						retval = ArtistSimilarityListInfo.CreateUnknown(artistInfo);
					else {
						artistId.Value = artistInfo.ArtistId.Id;
						using (var reader = CommandObj.ExecuteReader()) {
							//we expect exactly one hit - or none
							if (reader.Read())
								retval= new ArtistSimilarityListInfo(
									listID: new SimilarArtistsListId((long)reader[0]),
									artistInfo: artistInfo,
									lookupTimestamp: reader[2].CastDbObjectAsDateTime().Value,
									statusCode: (int?)reader[3].CastDbObjectAs<long?>(),
									similarArtists: new SimilarityList<ArtistId, ArtistId.Factory>(reader[4].CastDbObjectAs<byte[]>()) );
							else
								retval = ArtistSimilarityListInfo.CreateUnknown(artistInfo);
						}
					}
					trans.Commit();
					return retval;
				}
			}
		}
	}
}
