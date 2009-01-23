
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
FROM Artist A 
left join TopTracksList L on A.ArtistID = L.ArtistID
WHERE A.LowercaseArtist = @lowerArtist
ORDER BY L.LookupTimestamp DESC
LIMIT 1
";//we want the biggest timestamp first!
            }
        }
        DbParameter  lowerArtist;

        public static DateTime? DbValueTicksToDateTime(object dbval) {
            return dbval == DBNull.Value?
                (DateTime?)null:
                new DateTime((long)dbval, DateTimeKind.Utc);
        }

        public ArtistQueryInfo Execute(string artist) {
            lock (SyncRoot) {

                lowerArtist.Value = artist.ToLatinLowercase();
                using (var reader = CommandObj.ExecuteReader())//no transaction needed for a single select!
                {
                    //we expect exactly one hit - or none
                    if (reader.Read()) {
                        return new ArtistQueryInfo {
                            IsAlternateOf = reader[0] == DBNull.Value ? null : (int?)(long)reader[0],
                            LookupTimestamp = DbValueTicksToDateTime(reader[1]),
                            StatusCode = reader[2] == DBNull.Value ? null : (int?)(long)reader[2],
                        };
                    } else
                        return new ArtistQueryInfo {
                            IsAlternateOf = null,
                            LookupTimestamp = null,
                            StatusCode = null,
                        };
                }
            }
        }
    }
}
