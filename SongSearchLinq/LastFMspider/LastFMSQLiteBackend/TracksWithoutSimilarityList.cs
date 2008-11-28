
using System.Collections.Generic;
using System.Data.Common;
namespace LastFMspider.LastFMSQLiteBackend {
    public class TracksWithoutSimilarityList : AbstractLfmCacheQuery
    {
        public TracksWithoutSimilarityList(LastFMSQLiteCache lfm) : base(lfm) {
            limitRowCount = DefineParameter("@limitRowCount");
        }
        DbParameter limitRowCount;
        protected override string CommandText {
            get { return @"
SELECT T.TrackID, A.FullArtist, T.FullTitle, T.LookupTimestamp 
FROM Track T, Artist A 
WHERE T.LookupTimestamp IS NULL AND T.ArtistID = A.ArtistID
LIMIT @limitRowCount 
            ";
            }
        }

        public CachedTrack[] Execute(int limitRowCount) {
            List<CachedTrack> tracks = new List<CachedTrack>();
            this.limitRowCount.Value = limitRowCount;
            using (var reader = CommandObj.ExecuteReader()) {//no transaction needed for a single select!
                while (reader.Read())
                    tracks.Add(new CachedTrack {
                        ID = (int)(long)reader[0],
                        SongRef = SongRef.Create((string)reader[1], (string)reader[2]),
                        LookupTimestamp = LookupSimilarityListAge.DbValueTicksToDateTime(reader[3]) //should be NULL
                    });

            }
            return tracks.ToArray();
        }
    }
}
