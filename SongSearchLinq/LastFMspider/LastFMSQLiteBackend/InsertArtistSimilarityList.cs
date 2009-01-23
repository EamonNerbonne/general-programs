
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
            statusCode = DefineParameter("@statusCode");
        }
        protected override string CommandText {
            get {
                return @"
INSERT INTO [SimilarArtistList] (ArtistID, LookupTimestamp,StatusCode) 
SELECT A.ArtistID, (@lookupTimestamp) AS LookupTimestamp, (@statusCode) AS StatusCode
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

        DbParameter lowerArtist, lookupTimestamp, statusCode;


        private static IEnumerable<T> DeNull<T>(IEnumerable<T> iter) { return iter == null ? Enumerable.Empty<T>() : iter; }
        public void Execute(ArtistSimilarityList simList) {
            lock (SyncRoot) {
                using (DbTransaction trans = Connection.BeginTransaction()) {
                    lfmCache.InsertArtist.Execute(simList.Artist);
                    lfmCache.UpdateArtistCasing.Execute(simList.Artist);
                    int listID;
                    lowerArtist.Value = simList.Artist.ToLatinLowercase();
                    lookupTimestamp.Value = simList.LookupTimestamp.Ticks;
                    statusCode.Value = simList.StatusCode;
                    using (var reader = CommandObj.ExecuteReader()) {
                        if (reader.Read()) { //might need to do reader.NextResult();
                            listID = (int)(long)reader[0];
                        } else {
                            throw new Exception("Command failed???");
                        }
                    }

                    foreach (var similarArtist in simList.Similar) {
                        lfmCache.InsertArtistSimilarity.Execute(listID, similarArtist.Artist, similarArtist.Rating);
                        lfmCache.UpdateArtistCasing.Execute(similarArtist.Artist);
                    }
                    trans.Commit();
                }
            }
        }


    }
}
