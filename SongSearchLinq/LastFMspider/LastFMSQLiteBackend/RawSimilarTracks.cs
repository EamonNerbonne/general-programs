using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace LastFMspider.LastFMSQLiteBackend {
    public struct SimilarTrackRow {
        public int TrackA,TrackB;
        public float Rating;
    }
    public class RawSimilarTracks : AbstractLfmCacheQuery {
        public RawSimilarTracks(LastFMSQLiteCache lfm) : base(lfm) { }
        protected override string CommandText {
            get { return @"SELECT TrackA, TrackB, Rating FROM SimilarTrack"; }
        }

        public IEnumerable<SimilarTrackRow> Execute(bool printProgress) {
           // using (var transaction = Connection.BeginTransaction()) {// we need a transaction to ensure that the count is accurate and no buffer overflow occurs.
                int similarCount = lfmCache.CountRoughSimilarities.Execute();
  //              SimilarTrackRow[] similarTracks = new SimilarTrackRow[similarCount];
                int i = 0;
                DateTime start = DateTime.Now, last = DateTime.Now;
                using (var reader = CommandObj.ExecuteReader()) {
                    while (reader.Read() ) {
                        yield return new SimilarTrackRow {
                            TrackA = (int)(long)reader[0],
                            TrackB = (int)(long)reader[1],
                            Rating = (float)reader[2]
                        };
                        i++;
                        if (printProgress && DateTime.Now - last > TimeSpan.FromSeconds(1.0)) {
                            Console.Write("{0:g3}% ", (long)i * (double)100 / (double)similarCount);
                            last = DateTime.Now;
                        }
                    }
                    
               }
              //  Debug.Assert(i == similarCount, "Warning, there were " + (i < similarCount ? "fewer" : "more") + " records in the result set than expected.");
           // }
        }
    }
}
