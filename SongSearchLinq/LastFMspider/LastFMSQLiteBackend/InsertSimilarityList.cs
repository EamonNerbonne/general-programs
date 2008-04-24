using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
    public class InsertSimilarityList:AbstractLfmCacheOperation {
        public InsertSimilarityList(LastFMSQLiteCache lfm) : base(lfm) { }

        private static IEnumerable<T> DeNull<T>(IEnumerable<T> iter) { return iter == null ? Enumerable.Empty<T>() : iter; }
        public void Execute(SongSimilarityList list, DateTime? lookupTimestamp) {
            if (list == null) return;
            var tracks = DeNull(list.similartracks).Select(sim => sim.similarsong).ToList();
            tracks.Add(list.songref);

            DateTime timestamp = lookupTimestamp ?? DateTime.UtcNow;

            using (DbTransaction trans = Connection.BeginTransaction()) {
                DateTime? oldTime = lfmCache. LookupSimilarityListAge.Execute(list.songref);
                if (oldTime != null) {
                    if ((DateTime)oldTime >= timestamp)
                        return;
                    else
                        lfmCache. DeleteSimilaritiesOf.Execute(list.songref);
                }


                foreach (var songref in tracks) lfmCache.InsertTrack.Execute(songref);
                foreach (var similartrack in DeNull(list.similartracks)) lfmCache. InsertSimilarity.Execute(list.songref, similartrack.similarsong, similartrack.similarity);
                lfmCache. UpdateTrackTimestamp.Execute(list.songref, timestamp);
                trans.Commit();
            }
        }


    }
}
