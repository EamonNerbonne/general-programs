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
			trackID = DefineParameter("@trackID");
            lookupTimestamp = DefineParameter("@lookupTimestamp");
            statusCode = DefineParameter("@statusCode");
        }
        DbParameter trackID, lookupTimestamp,statusCode;
        protected override string CommandText {
            get {
                return @"
INSERT INTO [SimilarTrackList] (TrackID, LookupTimestamp,StatusCode) 
VALUES (@trackID, @lookupTimestamp, @statusCode);

SELECT L.ListID
FROM SimilarTrackList L
WHERE L.TrackID = @trackID
AND L.LookupTimestamp = @lookupTimestamp
";
            }
        }

        public void Execute(SongSimilarityList simList) {
            lock (SyncRoot) {
                using (DbTransaction trans = Connection.BeginTransaction()) {
                    long listID;
					trackID.Value = lfmCache.InsertTrack.Execute(simList.songref);
                    lookupTimestamp.Value = simList.LookupTimestamp.Ticks;
                    statusCode.Value = simList.StatusCode;

                    using (var reader = CommandObj.ExecuteReader()) {
                        if (reader.Read()) { //might need to do reader.NextResult()? guess not.
                            listID = (int)(long)reader[0];
                        } else {
                            throw new Exception("Command failed???");
                        }
                    }

					if (simList.LookupTimestamp > DateTime.Now - TimeSpan.FromDays(1.0)) {
						lfmCache.TrackSetCurrentSimList.Execute(listID); //presume if this is recently downloaded, then it's the most current.
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
