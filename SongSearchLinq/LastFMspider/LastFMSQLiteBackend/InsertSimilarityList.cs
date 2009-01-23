using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
    public class InsertSimilarityList : AbstractLfmCacheQuery
    {
        public InsertSimilarityList(LastFMSQLiteCache lfm)
            : base(lfm) {
            lowerArtist = DefineParameter("@lowerArtist");
            lookupTimestamp = DefineParameter("@lookupTimestamp");
            lowerTitle = DefineParameter("@lowerTitle");
            statusCode = DefineParameter("@statusCode");
        }
        DbParameter lowerArtist,lowerTitle, lookupTimestamp,statusCode;
        protected override string CommandText {
            get {
                return @"
INSERT INTO [SimilarTrackList] (TrackID, LookupTimestamp,StatusCode) 
SELECT T.TrackID, (@lookupTimestamp) AS LookupTimestamp, (@statusCode) AS StatusCode
FROM Artist A,Track T
WHERE A.LowercaseArtist = @lowerArtist
AND A.ArtistID = T.ArtistID
AND T.LowercaseTitle = @lowerTitle
;

SELECT L.ListID
FROM SimilarTrackList L, Artist A, Track T
WHERE A.LowercaseArtist = @lowerArtist
AND A.ArtistID = T.ArtistID
AND T.LowercaseTitle = @lowerTitle
AND L.TrackID = T.TrackID
AND L.LookupTimestamp = @lookupTimestamp
";
            }
        }

        private static IEnumerable<T> DeNull<T>(IEnumerable<T> iter) { return iter == null ? Enumerable.Empty<T>() : iter; }
        public void Execute(SongSimilarityList simList) {
            lock (SyncRoot) {
                using (DbTransaction trans = Connection.BeginTransaction()) {
                    lfmCache.InsertTrack.Execute(simList.songref);
                    int listID;
                    lowerArtist.Value = simList.songref.Artist.ToLatinLowercase();
                    lowerTitle.Value = simList.songref.Title.ToLatinLowercase();
                    lookupTimestamp.Value = simList.LookupTimestamp.Ticks;
                    statusCode.Value = simList.StatusCode;

                    using (var reader = CommandObj.ExecuteReader()) {
                        if (reader.Read()) { //might need to do reader.NextResult()? guess not.
                            listID = (int)(long)reader[0];
                        } else {
                            throw new Exception("Command failed???");
                        }
                    }

                    foreach (var similarTrack in simList.similartracks) {
                        lfmCache.InsertSimilarity.Execute(listID, similarTrack.similarsong, similarTrack.similarity);
                        lfmCache.UpdateTrackCasing.Execute(similarTrack.similarsong);
                    }
                    trans.Commit();
                }
            }
        }


    }
}
