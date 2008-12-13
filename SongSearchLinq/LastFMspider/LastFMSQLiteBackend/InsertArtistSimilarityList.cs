
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
    public class InsertArtistSimilarityList : AbstractLfmCacheQuery
    {
        public InsertArtistSimilarityList(LastFMSQLiteCache lfm) : base(lfm) { 
         lowerArtist = DefineParameter("@lowerArtist");
            lookupTimestamp = DefineParameter("@lookupTimestamp");
        }
        protected override string CommandText {
            get {
                return @"
INSERT INTO [SimilarArtistList] (ArtistID, LookupTimestamp) 
SELECT A.ArtistID, (@lookupTimestamp) AS LookupTimestamp
FROM Artist A
WHERE A.LowercaseArtist = @lowerArtist;

SELECT L.ListID
FROM SimilarArtistList L, Artist A
WHERE A.LowercaseArtist = @lowerArtist
AND L.ArtistID = A.ArtistID
AND L.LookupTimestamp = @lookupTimestamp
";
            }
        }

        DbParameter lowerArtist, lookupTimestamp;


        private static IEnumerable<T> DeNull<T>(IEnumerable<T> iter) { return iter == null ? Enumerable.Empty<T>() : iter; }
        public void Execute(ArtistSimilarityList simList) {
            using (DbTransaction trans = Connection.BeginTransaction()) {
                lfmCache.InsertArtist.Execute(simList.Artist);
                int listID;
                lowerArtist.Value = simList.Artist.ToLowerInvariant();
                lookupTimestamp.Value = simList.LookupTimestamp.Ticks;
                using (var reader = CommandObj.ExecuteReader())
                {
                    if (reader.Read()) { //might need to do reader.NextResult();
                        listID = (int)(long)reader[0];
                    } else {
                        throw new Exception("Command failed???");
                    }
                }

                foreach (var similarArtist in simList.Similar) {
                    lfmCache.InsertArtistSimilarity.Execute(listID, similarArtist.Artist, similarArtist.Rating);
                }
                trans.Commit();
            }
        }


    }
}
