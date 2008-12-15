using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.SQLite;

namespace LastFMspider.LastFMSQLiteBackend
{
    public struct SimilarArtist
    {
        public string Artist;
        public double Rating;
    }
    public class ArtistSimilarityList
    {
        public DateTime LookupTimestamp;
        public string Artist;
        public SimilarArtist[] Similar;
    }

    public class LookupArtistSimilarityList : AbstractLfmCacheQuery
    {
        public LookupArtistSimilarityList(LastFMSQLiteCache lfmCache)
            : base(lfmCache) {
            lowerArtist = DefineParameter("@lowerArtist");
            ticks = DefineParameter("@ticks");
        }
        protected override string CommandText {
            get {
                return @"
SELECT B.FullArtist, S.Rating
FROM Artist A 
join SimilarArtistList L on A.ArtistID = L.ArtistID
join SimilarArtist S on L.ListID = S.ListID
join Artist B on S.ArtistB = B.ArtistID
WHERE A.LowercaseArtist = @lowerArtist
AND L.LookupTimestamp = @ticks
";
            }
        }
        DbParameter lowerArtist,ticks;

        public ArtistSimilarityList Execute(string artist) {
            using (var trans = Connection.BeginTransaction()) {
                DateTime? age = lfmCache.LookupArtistSimilarityListAge.Execute(artist);
                if (null == age)
                    return null;

                lowerArtist.Value = artist.ToLatinLowercase();
                ticks.Value = age.Value.Ticks;//we want the newest one!

                List<SimilarArtist> similarto = new List<SimilarArtist>();
                using (var reader = CommandObj.ExecuteReader()) {
                    while (reader.Read())
                        similarto.Add(new SimilarArtist {
                            Artist = (string)reader[0],
                            Rating = (float)reader[1],
                        });
                }
                var retval = new ArtistSimilarityList {
                    Artist = artist,
                    Similar = similarto.ToArray(),
                    LookupTimestamp = age.Value
                };
                return retval;
            }
        }


    }
}
