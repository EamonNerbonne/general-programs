
using System.Collections.Generic;
using System.Data.Common;
using System;
namespace LastFMspider.LastFMSQLiteBackend
{
    public class CachedArtist
    {
        public int ArtistID;
        public string ArtistName;
        public DateTime? LookupTimestamp;
    }
    public class ArtistsWithoutSimilarityList : AbstractLfmCacheQuery
    {
        public ArtistsWithoutSimilarityList(LastFMSQLiteCache lfm)
            : base(lfm) {
            limitRowCount = DefineParameter("@limitRowCount");
			minAge = DefineParameter("@minAge");
        }
        DbParameter limitRowCount, minAge;
        protected override string CommandText {
            get { return @"
SELECT A.ArtistID, A.FullArtist
FROM Artist A 
WHERE A.IsAlternateOf IS NULL AND 
    (
      A.CurrentSimilarArtistList IS NULL 
    OR
      @minAge >= (
          SELECT L.LookupTimestamp
          FROM SimilarArtistList L 
          WHERE A.CurrentSimilarArtistList = L.ListID)
    )
LIMIT @limitRowCount
"; }
        }

        public CachedArtist[] Execute(int limitRowCount, DateTime minAge) {
            List<CachedArtist> tracks = new List<CachedArtist>();
            lock (SyncRoot) {
                this.limitRowCount.Value = limitRowCount;
				this.minAge.Value = minAge.ToUniversalTime().Ticks; //should be in universal time anyhow...

                using (var reader = CommandObj.ExecuteReader()) {//no transaction needed for a single select!
                    while (reader.Read())
                        tracks.Add(new CachedArtist {
                            ArtistID = (int)(long)reader[0],
                            ArtistName = (string)reader[1],
                            LookupTimestamp = null 
                        });

                }
            }
            return tracks.ToArray();
        }
    }
}
