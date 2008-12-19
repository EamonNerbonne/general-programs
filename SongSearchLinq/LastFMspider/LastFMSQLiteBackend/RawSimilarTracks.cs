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
            get { return @"SELECT L.TrackID, S.TrackB, S.Rating FROM SimilarTrackList L, SimilarTrack S WHERE S.ListID = L.ListID"; }
        }

        public void Execute(bool printProgress, Func<int,int> AcceptCount, Action<int,SimilarTrackRow> AcceptNthRow) {
            lock (SyncRoot) {

                using (var transaction = Connection.BeginTransaction()) {
                    int simCount = lfmCache.CountSimilarities.Execute();

                    simCount = AcceptCount(simCount);
                    int i = 0;
                    DateTime start = DateTime.Now, last = DateTime.Now;
                    using (var reader = CommandObj.ExecuteReader()) {
                        while (reader.Read() && i < simCount) {
                            AcceptNthRow(i, new SimilarTrackRow {
                                TrackA = (int)(long)reader[0],
                                TrackB = (int)(long)reader[1],
                                Rating = (float)reader[2]
                            });
                            i++;
                            if (printProgress && DateTime.Now - last > TimeSpan.FromSeconds(1.0)) {
                                Console.Write("{0:g3}% ", (long)i * (double)100 / (double)simCount);
                                last = DateTime.Now;
                            }
                        }
                    }
                    Debug.Assert(i == simCount, "The counted number of similarity does not equal the number retrieved!");
                }
            }
        }
    }
}
