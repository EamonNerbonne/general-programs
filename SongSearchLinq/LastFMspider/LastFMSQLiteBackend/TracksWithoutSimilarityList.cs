
using System.Collections.Generic;
using System.Data.Common;
namespace LastFMspider.LastFMSQLiteBackend {
	public class TracksWithoutSimilarityList : AbstractLfmCacheQuery {
		public TracksWithoutSimilarityList(LastFMSQLiteCache lfm)
			: base(lfm) {
			limitRowCount = DefineParameter("@limitRowCount");
		}
		DbParameter limitRowCount;
		protected override string CommandText {
			get {
				return @"
SELECT T.TrackID, A.FullArtist, T.FullTitle
FROM  Track T, Artist A
WHERE T.CurrentSimilarTrackList IS NULL
AND A.ArtistID = T.ArtistID
LIMIT @limitRowCount 
            ";
			}
		}

		public CachedTrack[] Execute(int limitRowCount) {
			List<CachedTrack> tracks = new List<CachedTrack>();
			lock (SyncRoot) {

				this.limitRowCount.Value = limitRowCount;
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
