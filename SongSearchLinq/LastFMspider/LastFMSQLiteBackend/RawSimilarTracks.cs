using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public SimilarTrackRow[] Execute() {
            List<SimilarTrackRow> similarTracks = new List<SimilarTrackRow>();
            using (var reader = CommandObj.ExecuteReader()) {//no transaction needed for a single select!
                while (reader.Read())
                    similarTracks.Add(new SimilarTrackRow {
                        TrackA = (int)(long)reader[0],
                        TrackB = (int)(long)reader[1],
                         Rating = (float)reader[2]
                    });

            }
            return similarTracks.ToArray();
        }
    }
}
