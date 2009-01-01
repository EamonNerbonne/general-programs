using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
    public struct ListStatus
    {
        public DateTime Timestamp;
        public int? StatusCode;
        public bool IsOK() { return (StatusCode ?? 200) < 400; }
    }
    public class LookupSimilarityListAge : AbstractLfmCacheQuery {
        public LookupSimilarityListAge(LastFMSQLiteCache lfmCache)
            : base(lfmCache) {
            lowerArtist = DefineParameter("@lowerArtist");
            lowerTitle = DefineParameter("@lowerTitle");
        }
        protected override string CommandText {
            get {
                return @"
SELECT L.LookupTimestamp, L.StatusCode 
FROM Artist A,
     Track T, 
     SimilarTrackList L
WHERE A.LowercaseArtist = @lowerArtist
AND T.ArtistID=A.ArtistID
AND T.LowercaseTitle = @lowerTitle
AND L.TrackID = T.TrackID
ORDER BY L.LookupTimestamp DESC
LIMIT 1
";
            }
        }
        DbParameter lowerTitle, lowerArtist;

        public static DateTime? DbValueTicksToDateTime(object dbval) {
            return DBNull.Value==dbval?null:(DateTime?) new DateTime((long)dbval, DateTimeKind.Utc);
        }

        public ListStatus? Execute(SongRef songref) {
            lock (SyncRoot) {

                lowerArtist.Value = songref.Artist.ToLatinLowercase();
                lowerTitle.Value = songref.Title.ToLatinLowercase();
                using (var reader = CommandObj.ExecuteReader())//no transaction needed for a single select!
                {
                    //we expect exactly one hit - or none
                    if (reader.Read()) {
                        return new ListStatus { 
                            Timestamp = DbValueTicksToDateTime(reader[0]).Value, 
                            StatusCode = reader[1] == DBNull.Value ? (int?)null : (int)(long)reader[1] };
                    } else
                        return null;
                }
            }
        }
    }
}
