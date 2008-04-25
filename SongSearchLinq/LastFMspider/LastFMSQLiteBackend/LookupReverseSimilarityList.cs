using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.SQLite;

namespace LastFMspider.LastFMSQLiteBackend {
    public class LookupReverseSimilarityList : AbstractLfmCacheQuery {
        public LookupReverseSimilarityList(LastFMSQLiteCache lfmCache)
            : base(lfmCache) {
            lowerArtist = DefineParameter("@lowerArtist");
            lowerTitle = DefineParameter("@lowerTitle");
        }
        protected override string CommandText {
            get {
                return @"
SELECT S.Rating, A.FullArtist, T.FullTitle FROM
  SimilarTrack S, Artist A, Track T, Track Torig, Artist Aorig
  WHERE
    Aorig.LowercaseArtist=@lowerArtist AND
    Aorig.ArtistID = Torig.ArtistID AND
    Torig.LowercaseTitle = @lowerTitle AND
    S.TrackB = Torig.TrackID AND
    T.TrackID = S.TrackA AND
    A.ArtistID = T.ArtistID
  ORDER BY S.Rating DESC
";
            }
        }
        DbParameter lowerTitle, lowerArtist;

        public SongSimilarityList Execute(SongRef songref) {
            lowerArtist.Value = songref.Artist.ToLowerInvariant();
            lowerTitle.Value = songref.Title.ToLowerInvariant();
            List<SimilarTrack> similarto = new List<SimilarTrack>();
            using (var reader = CommandObj.ExecuteReader())//no transaction needed for a single select!
                {
                while (reader.Read())
                    similarto.Add(new SimilarTrack {
                        similarity = (float)reader[0],
                        similarsong = SongRef.Create((string)reader[1], (string)reader[2])
                    });
            }
            var retval = new SongSimilarityList {
                songref = songref,
                similartracks = similarto.ToArray()
            };
            return retval;
        }


    }
}
