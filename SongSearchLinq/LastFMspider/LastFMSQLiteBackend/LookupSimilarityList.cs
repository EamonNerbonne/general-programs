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
			listId = DefineParameter("@listId");
        }
        protected override string CommandText {
            get {
                return @"
SELECT BA.FullArtist, BT.FullTitle, S.Rating
FROM SimilarTrack S,
     Track BT,
     Artist BA
WHERE S.ListID = @listId
AND BT.TrackID = S.TrackB
AND BA.ArtistID = BT.ArtistID
ORDER BY S.Rating DESC
";
            }
        }
        DbParameter listId;

        public SongSimilarityList Execute(TrackSimilarityListInfo list) {
			if (list.ListID==null || list.SongRef==null) return null;

            lock (SyncRoot) {
                using (var trans = Connection.BeginTransaction()) {
					listId.Value = list.ListID;

                    List<SimilarTrack> similarto = new List<SimilarTrack>();
                    using (var reader = CommandObj.ExecuteReader()) {
                        while (reader.Read())
                            similarto.Add(new SimilarTrack {
                                similarity = (float)reader[2],
                                similarsong = SongRef.Create((string)reader[0], (string)reader[1])
                            });
                    }
                    var retval = new SongSimilarityList {
                        songref = list.SongRef,
                        similartracks = similarto.ToArray(),
						LookupTimestamp = list.LookupTimestamp.Value,
						StatusCode = list.StatusCode,
                    };
                    return retval;
                }
            }
        }
    }
}
