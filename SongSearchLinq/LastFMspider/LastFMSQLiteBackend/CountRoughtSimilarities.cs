using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LastFMspider.LastFMSQLiteBackend
{
    public class CountRoughSimilarities : AbstractLfmCacheQuery
    {
        public CountRoughSimilarities(LastFMSQLiteCache lfmCache) : base(lfmCache) { }
        protected override string CommandText { get { return @"SELECT max(SimilarTrackID) FROM  SimilarTrack"; } }

        public int Execute() {
            lock (SyncRoot) {
                using (var reader = CommandObj.ExecuteReader()) { //no transaction needed for a single select!
                    if (reader.Read())
                        return (int)(long)reader[0];
                    else return 0;
                }
            }
        }
    }
}
