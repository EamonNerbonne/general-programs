using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
    public class InsertSimilarityList:AbstractLfmCacheOperation {
        public InsertSimilarityList(LastFMSQLiteCache lfm) : base(lfm) { }

        private static IEnumerable<T> DeNull<T>(IEnumerable<T> iter) { return iter == null ? Enumerable.Empty<T>() : iter; }
        public void Execute(SongSimilarityList list) {
            if (list == null) return;
            
            DateTime timestamp = list.LookupTimestamp;

            using (DbTransaction trans = Connection.BeginTransaction()) {
                DateTime? oldTime = lfmCache. LookupSimilarityListAge.Execute(list.songref);
                if (oldTime != null) {
                    if ((DateTime)oldTime >= timestamp)
                        return;
                    else
                        lfmCache. DeleteSimilaritiesOf.Execute(list.songref);
                }

                lfmCache.InsertTrack.Execute(list.songref);
                int trackID = lfmCache.LookupTrackID.Execute(list.songref).Value;//must exist due to insert
                foreach (var similartrack in DeNull(list.similartracks)) {
                    lfmCache.InsertSimilarity.Execute(trackID, similartrack.similarsong, similartrack.similarity);
                }
                lfmCache. UpdateTrackTimestamp.Execute(list.songref, timestamp);
                trans.Commit();
            }
        }


    }
}
