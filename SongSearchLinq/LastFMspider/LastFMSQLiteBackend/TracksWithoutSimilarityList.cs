
using System.Collections.Generic;
using System.Data.Common;
using System;
namespace LastFMspider.LastFMSQLiteBackend {
	public class TracksWithoutSimilarityList : AbstractLfmCacheQuery {
		public TracksWithoutSimilarityList(LastFMSQLiteCache lfm)
			: base(lfm) {
			limitRowCount = DefineParameter("@limitRowCount");
			maxDate = DefineParameter("@maxDate");
		}
		DbParameter limitRowCount,maxDate;
		protected override string CommandText {
			get {
				return @"
SELECT T.TrackID, A.FullArtist, T.FullTitle
FROM  Track T, Artist A
WHERE (T.CurrentSimilarTrackList IS NULL OR T.CurrentSimilarTrackList <= @maxDate)
AND A.ArtistID = T.ArtistID
LIMIT @limitRowCount 
            ";
			}
		}

		public CachedTrack[] Execute(int limitRowCount, DateTime maxDate) {
			List<CachedTrack> tracks = new List<CachedTrack>();
			lock (SyncRoot) {

				this.limitRowCount.Value = limitRowCount;
				this.maxDate.Value = maxDate.ToUniversalTime().Ticks;
				using (var reader = CommandObj.ExecuteReader()) {//no transaction needed for a single select!
					while (reader.Read())
						tracks.Add(new CachedTrack {
							ID = new TrackId(reader[0].CastDbObjectAs<long>()),
							SongRef = SongRef.Create((string)reader[1], (string)reader[2]),
						});

				}
			}
			return tracks.ToArray();
		}
	}
}
