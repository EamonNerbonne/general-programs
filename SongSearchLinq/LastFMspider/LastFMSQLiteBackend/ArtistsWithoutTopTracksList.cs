using System.Collections.Generic;
using System.Data.Common;
using System;
namespace LastFMspider.LastFMSQLiteBackend {
	public class ArtistsWithoutTopTracksList : AbstractLfmCacheQuery {
		public ArtistsWithoutTopTracksList(LastFMSQLiteCache lfm)
			: base(lfm) {
			limitRowCount = DefineParameter("@limitRowCount");
			maxDate = DefineParameter("@maxDate");
		}
		DbParameter limitRowCount, maxDate;
		protected override string CommandText {
			get {
				return @"
	SELECT A.ArtistID, A.FullArtist, A.CurrentTopTracksListTimestamp
	FROM Artist A 
	WHERE A.CurrentTopTracksListTimestamp IS NULL AND A.IsAlternateOf IS NULL
LIMIT @limitRowCount;
	SELECT A.ArtistID, A.FullArtist, A.CurrentTopTracksListTimestamp
	FROM Artist A 
	WHERE A.CurrentTopTracksListTimestamp <= @maxDate AND A.IsAlternateOf IS NULL
LIMIT @limitRowCount;
";
			}
		}

		public CachedArtist[] Execute(int limitRowCount, DateTime maxDate) {
			List<CachedArtist> artists = new List<CachedArtist>();
			lock (SyncRoot) {
				this.limitRowCount.Value = limitRowCount;
				this.maxDate.Value = maxDate.ToUniversalTime().Ticks;

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
