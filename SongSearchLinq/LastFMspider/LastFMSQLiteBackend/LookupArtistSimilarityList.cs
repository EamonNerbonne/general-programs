using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.SQLite;

namespace LastFMspider.LastFMSQLiteBackend {

	public class LookupArtistSimilarityList : AbstractLfmCacheQuery {
		public LookupArtistSimilarityList(LastFMSQLiteCache lfmCache)
			: base(lfmCache) {
			lowerArtist = DefineParameter("@lowerArtist");
			ticks = DefineParameter("@ticks");
		}
		protected override string CommandText {
			get {
				return @"
SELECT B.FullArtist, S.Rating
FROM Artist A 
join SimilarArtistList L on L.ArtistID = A.ArtistID
join SimilarArtist S on S.ListID = L.ListID
join Artist B on B.ArtistID = S.ArtistB
WHERE A.LowercaseArtist = @lowerArtist
AND L.LookupTimestamp = @ticks
";
			} //TODO: make this faster: if I write the where clause +implicit where clause of the joins in another order, is that more efficient?  Also: maybe encode sim-lists as one column
		}
		DbParameter lowerArtist, ticks;

		public ArtistSimilarityList Execute(string artist) {
			lock (SyncRoot) {

				using (var trans = Connection.BeginTransaction()) {
					ArtistQueryInfo
						info = lfmCache.LookupArtistSimilarityListAge.Execute(artist);
					if (info.IsAlternateOf.HasValue)
						return Execute(lfmCache.LookupArtist.Execute(info.IsAlternateOf));
					if (!info.LookupTimestamp.HasValue)
						return null;
					DateTime age = info.LookupTimestamp.Value;

					lowerArtist.Value = artist.ToLatinLowercase();
					ticks.Value = age.Ticks;//we want the newest one!

					List<SimilarArtist> similarto = new List<SimilarArtist>();
					using (var reader = CommandObj.ExecuteReader()) {
						while (reader.Read())
							similarto.Add(new SimilarArtist {
								Artist = (string)reader[0],
								Rating = (float)reader[1],
							});
					}
					var retval = new ArtistSimilarityList {
						Artist = artist,
						Similar = similarto.ToArray(),
						LookupTimestamp = age,
						StatusCode = info.StatusCode,

					};
					return retval;
				}
			}
		}


	}
}
