using System.Collections.Generic;
using System.Data.Common;
using System;
namespace LastFMspider.LastFMSQLiteBackend {
	public class CachedArtist {
		public ArtistId ArtistID;
		public string ArtistName;
		public DateTime? LookupTimestamp;
	}
	public class ArtistsWithoutSimilarityList : AbstractLfmCacheQuery {
		public ArtistsWithoutSimilarityList(LastFMSQLiteCache lfm)
			: base(lfm) {
			limitRowCountParam = DefineParameter("@limitRowCount");
			maxDateParam = DefineParameter("@maxDate");
		}
		readonly DbParameter limitRowCountParam, maxDateParam;
		protected override string CommandText {
			get {
				return @"
	SELECT A.ArtistID, A.FullArtist, A.CurrentSimilarArtistListTimestamp
	FROM Artist A 
	WHERE A.IsAlternateOf IS NULL AND A.CurrentSimilarArtistListTimestamp IS NULL
LIMIT @limitRowCount;
	SELECT A.ArtistID, A.FullArtist, A.CurrentSimilarArtistListTimestamp
	FROM Artist A 
	WHERE A.IsAlternateOf IS NULL AND A.CurrentSimilarArtistListTimestamp <= @maxDate
LIMIT @limitRowCount;
";
			}
		}

		public CachedArtist[] Execute(int limitRowCount, DateTime maxDate) {
			List<CachedArtist> artists = new List<CachedArtist>();
			lock (SyncRoot) {
				limitRowCountParam.Value = limitRowCount;
				maxDateParam.Value = maxDate.ToUniversalTime().Ticks; //should be in universal time anyhow...

				using (var reader = CommandObj.ExecuteReader()) {
					while (artists.Count < limitRowCount && (reader.Read() || (reader.NextResult() && reader.Read())))
						artists.Add(new CachedArtist {
							ArtistID = new ArtistId((long)reader[0]),
							ArtistName = (string)reader[1],
							LookupTimestamp = reader[2].CastDbObjectAsDateTime()
						});

				}
			}
			return artists.ToArray();
		}
	}
}
