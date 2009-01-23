using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.SQLite;

namespace LastFMspider.LastFMSQLiteBackend
{
    public class LookupSimilarityList : AbstractLfmCacheQuery
    {
        public LookupSimilarityList(LastFMSQLiteCache lfmCache)
            : base(lfmCache) {
            lowerArtist = DefineParameter("@lowerArtist");
            lowerTitle = DefineParameter("@lowerTitle");
            ticks = DefineParameter("@ticks");
        }
        protected override string CommandText {
            get {
                return @"
SELECT BA.FullArtist, BT.FullTitle, S.Rating
FROM Artist A, 
     Track T,
     SimilarTrackList TS,
     SimilarTrack S,
     Track BT,
     Artist BA
WHERE A.LowercaseArtist = @lowerArtist
AND T.ArtistID = A.ArtistID
AND TS.TrackID = T.TrackID
AND TS.LookupTimestamp = @ticks
AND S.ListID = TS.ListID
AND BT.TrackID = S.TrackB
AND BA.ArtistID = BT.ArtistID
ORDER BY S.Rating DESC
";
            }
        }
        DbParameter lowerTitle, lowerArtist, ticks;

        public SongSimilarityList Execute(SongRef songref) {
            lock (SyncRoot) {
                using (var trans = Connection.BeginTransaction()) {
                    ListStatus listStatus = lfmCache.LookupSimilarityListAge.Execute(songref);

                    if (!listStatus.LookupTimestamp.HasValue) return null;
                    lowerArtist.Value = songref.Artist.ToLatinLowercase();
                    lowerTitle.Value = songref.Title.ToLatinLowercase();
                    ticks.Value = listStatus.LookupTimestamp.Value.Ticks;

                    List<SimilarTrack> similarto = new List<SimilarTrack>();
                    using (var reader = CommandObj.ExecuteReader()) {
                        while (reader.Read())
                            similarto.Add(new SimilarTrack {
                                similarity = (float)reader[2],
                                similarsong = SongRef.Create((string)reader[0], (string)reader[1])
                            });
                    }
                    var retval = new SongSimilarityList {
                        songref = songref,
                        similartracks = similarto.ToArray(),
                        LookupTimestamp = listStatus.LookupTimestamp.Value,
                        StatusCode = listStatus.StatusCode,
                    };
                    return retval;
                }
            }
        }
    }
}
