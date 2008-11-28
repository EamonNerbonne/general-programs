using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
    public class InsertArtistTopTracksList : AbstractLfmCacheQuery
    {
        public InsertArtistTopTracksList(LastFMSQLiteCache lfm)
            : base(lfm) { 
         lowerArtist = DefineParameter("@lowerArtist");
            lookupTimestamp = DefineParameter("@lookupTimestamp");
        }
        protected override string CommandText {
            get {
                return @"
INSERT INTO [TopTracksList] (ArtistID, LookupTimestamp) 
SELECT A.ArtistID, (@lookupTimestamp) AS LookupTimestamp
FROM Artist A
WHERE A.LowercaseArtist = @lowerArtist;

SELECT L.ListID,A.ArtistID
FROM TopTracksList L, Artist A
WHERE A.LowercaseArtist = @lowerArtist
AND L.ArtistID = A.ArtistID
AND L.LookupTimestamp = @lookupTimestamp
";
            
            }
        }

        DbParameter lowerArtist, lookupTimestamp;


        private static IEnumerable<T> DeNull<T>(IEnumerable<T> iter) { return iter == null ? Enumerable.Empty<T>() : iter; }
        public void Execute(ArtistTopTracksList toptracksList) {
            using (DbTransaction trans = Connection.BeginTransaction()) {
                lfmCache.InsertArtist.Execute(toptracksList.Artist);
                int listID;
                int artistID;
                lowerArtist.Value = toptracksList.Artist.ToLowerInvariant();
                lookupTimestamp.Value = toptracksList.LookupTimestamp.Ticks;
                using (var reader = CommandObj.ExecuteReader())
                {
                    if (reader.Read()) { //might need to do reader.NextResult();
                        listID = (int)(long)reader[0];
                        artistID = (int)(long)reader[1];
                    } else {
                        throw new Exception("Command failed???");
                    }
                }

                foreach (var toptrack in toptracksList.TopTracks) {
                    lfmCache.InsertArtistTopTrack.Execute(listID, artistID, toptrack.Track, toptrack.Reach);
                }
                trans.Commit();
            }
        }


    }
}
