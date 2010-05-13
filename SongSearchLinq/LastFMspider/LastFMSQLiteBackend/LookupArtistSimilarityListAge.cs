using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public struct ArtistQueryInfo {
		public readonly ArtistId IsAlternateOf;
		public readonly DateTime? LookupTimestamp;
		public readonly int? StatusCode;

		public ArtistQueryInfo(ArtistId IsAlternateOf, DateTime? LookupTimestamp, int? StatusCode) {
			this.IsAlternateOf = IsAlternateOf; this.LookupTimestamp = LookupTimestamp; this.StatusCode = StatusCode;
		}
		public static ArtistQueryInfo Default { get { return default(ArtistQueryInfo); } }
		// negative: non-problematic error (-1 == http404)
		// 0: no error, list is accurate
		// positive: list request error; list is empty but that might be an error.
		// 1: unknown exception occurred (DB locked?)
		// 2-22: WebException occured
		// 32: InvalidOperationException occurred.
	}

	public class LookupArtistSimilarityListAge : AbstractLfmCacheQuery {
		public LookupArtistSimilarityListAge(LastFMSQLiteCache lfmCache)
			: base(lfmCache) {
			lowerArtist = DefineParameter("@lowerArtist");
		}
		protected override string CommandText {
			get {
				return @"
SELECT A.IsAlternateOf, L.LookupTimestamp, L.StatusCode
FROM Artist A 
left join SimilarArtistList L on A.CurrentSimilarArtistList = L.ListID
WHERE A.LowercaseArtist = @lowerArtist
";
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
							StatusCode: (int?)reader[0].CastDbObjectAs<int?>()
						);
					} else
						return ArtistQueryInfo.Default;
				}
			}
		}
	}
}
