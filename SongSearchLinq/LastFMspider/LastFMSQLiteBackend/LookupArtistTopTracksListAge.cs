
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public class LookupArtistTopTracksListAge : AbstractLfmCacheQuery {
		public LookupArtistTopTracksListAge(LastFMSQLiteCache lfmCache)
			: base(lfmCache) {
			lowerArtist = DefineParameter("@lowerArtist");
		}
		protected override string CommandText {
			get {
				return @"
SELECT A.IsAlternateOf, L.LookupTimestamp, L.StatusCode 
FROM Artist A, TopTracksList L 
WHERE A.LowercaseArtist = @lowerArtist
AND L.ListID = A.CurrentTopTracksList
";//we want the biggest timestamp first!
			}
		}
		DbParameter lowerArtist;

		public ArtistQueryInfo Execute(string artist) {
			lock (SyncRoot) {

				lowerArtist.Value = artist.ToLatinLowercase();
				using (var reader = CommandObj.ExecuteReader())//no transaction needed for a single select!
                {
					//we expect exactly one hit - or none
					if (reader.Read()) {
						return new ArtistQueryInfo(
							IsAlternateOf: new ArtistId(reader[0].CastDbObjectAs<long?>()),
							LookupTimestamp: reader[1].CastDbObjectAsDateTime(),
							StatusCode: (int?)reader[2].CastDbObjectAs<long?>()
						);
					} else
						return ArtistQueryInfo.Default;
				}
			}
		}
	}
}
