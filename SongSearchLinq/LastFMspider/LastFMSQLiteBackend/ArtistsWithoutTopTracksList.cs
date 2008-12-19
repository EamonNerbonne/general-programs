﻿using System.Collections.Generic;
using System.Data.Common;
using System;
namespace LastFMspider.LastFMSQLiteBackend
{
    public class ArtistsWithoutTopTracksList : AbstractLfmCacheQuery
    {
        public ArtistsWithoutTopTracksList(LastFMSQLiteCache lfm)
            : base(lfm) {
            limitRowCount = DefineParameter("@limitRowCount");
        }
        DbParameter limitRowCount;
        protected override string CommandText {
            get { return @"
SELECT A.ArtistID, A.FullArtist, L.LookupTimestamp 
FROM Artist A left join TopTracksList L on A.ArtistID = L.ArtistID
WHERE   L.LookupTimestamp IS NULL
LIMIT @limitRowCount
"; }
        }

        public CachedArtist[] Execute(int limitRowCount) {
            List<CachedArtist> tracks = new List<CachedArtist>();
            lock (SyncRoot) {
                this.limitRowCount.Value = limitRowCount;
                using (var reader = CommandObj.ExecuteReader()) {//no transaction needed for a single select!
                    while (reader.Read())
                        tracks.Add(new CachedArtist {
                            ArtistID = (int)(long)reader[0],
                            ArtistName = (string)reader[1],
                            LookupTimestamp = LookupSimilarityListAge.DbValueTicksToDateTime(reader[2]) //should be NULL
                        });

                }
            }
            return tracks.ToArray();
        }
    }
}
