using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace LastFMspider.LastFMSQLiteBackend {
    public class SimilarTrackRow {
        public int TrackA,TrackB;
        public float Rating;
    }
    public class RawSimilarTracks : AbstractLfmCacheQuery {
        public RawSimilarTracks(LastFMSQLiteCache lfm) : base(lfm) { }
        protected override string CommandText {
            get { return @"SELECT TrackA, TrackB, Rating FROM SimilarTrack"; }
        }

        public SimilarTrackRow[] Execute(bool printProgress) {
            using (var transaction = Connection.BeginTransaction()) {// we need a transaction to ensure that the count is accurate and no buffer overflow occurs.
                int similarCount = lfmCache.CountSimilarities.Execute();
                SimilarTrackRow[] similarTracks = new SimilarTrackRow[similarCount];
                int i = 0;
                using (var reader = CommandObj.ExecuteReader()) {
                    while (reader.Read() ) {
                        similarTracks[i] = new SimilarTrackRow {
                            TrackA = (int)(long)reader[0],
                            TrackB = (int)(long)reader[1],
                            Rating = (float)reader[2]
                        };
                        i++;
                        if(printProgress && i*10 / similarCount != (i+1)*10 /similarCount)
                            Console.Write("{0}",i*10/similarCount);
                    }
                    
                }
                Debug.Assert(i == similarCount, "Warning, there were " + (i < similarCount ? "fewer" : "more") + " records in the result set than expected.");
            return similarTracks;
            }
        }
    }
}
