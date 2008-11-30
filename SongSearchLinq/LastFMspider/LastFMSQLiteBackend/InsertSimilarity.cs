using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
    public class InsertSimilarity:AbstractLfmCacheQuery {
        public InsertSimilarity(LastFMSQLiteCache lfm)
            : base(lfm) {

            rating = DefineParameter("@rating");

            trackA = DefineParameter("@trackA");

            trackB = DefineParameter("@trackB");
        }
        protected override string CommandText {
            get { return @"
INSERT OR REPLACE INTO [SimilarTrack] (TrackA, TrackB, Rating) 
VALUES (@trackA,trackB,@rating)
";
            }
        }

        DbParameter trackA, trackB, rating;
      


        public void Execute(int trackID, SongRef songRefB, double rating) {
            int? trackBid = lfmCache.LookupTrackID.Execute(songRefB);
            if (trackBid.HasValue)
                lfmCache.UpdateTrackCasing.Execute(songRefB);
            else {
                lfmCache.InsertTrack.Execute(songRefB);
                trackBid = lfmCache.LookupTrackID.Execute(songRefB);
            }
            this.rating.Value = rating;
            this.trackA.Value = trackID;
            this.trackB.Value = trackBid.Value;
            CommandObj.ExecuteNonQuery();
        }

    }
}
