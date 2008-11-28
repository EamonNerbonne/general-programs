
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
SELECT L.LookupTimestamp 
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

        public DateTime? Execute(string artist) {
            lowerArtist.Value = artist.ToLowerInvariant();
            using (var reader = CommandObj.ExecuteReader())//no transaction needed for a single select!
                {
                //we expect exactly one hit - or none
                if (reader.Read()) {
                    return DbValueTicksToDateTime(reader[0]);
                }
                else
                    return null;
            }
        }
    }
}
