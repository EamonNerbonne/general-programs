using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
    public class LookupSimilarityListAge : AbstractLfmCacheQuery {
        public LookupSimilarityListAge(LastFMSQLiteCache lfmCache)
            : base(lfmCache) {
            lowerArtist = DefineParameter("@lowerArtist");
            lowerTitle = DefineParameter("@lowerTitle");
        }
        protected override string CommandText {
            get {
                return @"
SELECT Torig.LookupTimestamp FROM
  Track Torig, Artist Aorig
  WHERE
    Aorig.LowercaseArtist=@lowerArtist AND
    Aorig.ArtistID = Torig.ArtistID AND
    Torig.LowercaseTitle = @lowerTitle
";
            }
        }
        DbParameter lowerTitle, lowerArtist;

        public DateTime? Execute(SongRef songref) {
            lowerArtist.Value = songref.Artist.ToLowerInvariant();
            lowerTitle.Value = songref.Title.ToLowerInvariant();
            using (var reader = CommandObj.ExecuteReader())//no transaction needed for a single select!
                {
                //we expect exactly one hit - or none
                if (reader.Read()) {
                    long? ticks =  (reader[0]==DBNull.Value?(long?)null:(long?)reader[0]);
                    if (ticks == null)
                        return null;
                    else return new DateTime((long)ticks, DateTimeKind.Utc);
                }
                else
                    return null;
            }
        }
    }
}
