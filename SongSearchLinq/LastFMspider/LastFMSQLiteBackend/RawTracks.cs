using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LastFMspider.LastFMSQLiteBackend {
    public class TrackRow {
        public int TrackID;
        public int ArtistID;
        public string FullTitle;
        public string LowercaseTitle;
        public DateTime? LookupTimestamp;
    }
    public class RawTracks : AbstractLfmCacheQuery {
        public RawTracks(LastFMSQLiteCache lfm) : base(lfm) { }
        protected override string CommandText {
            get { return @"SELECT TrackID, ArtistID, FullTitle, LowercaseTitle, LookupTimestamp FROM Track"; }
        }

        public TrackRow[] Execute() {
            List<TrackRow> tracks = new List<TrackRow>();
            using (var reader = CommandObj.ExecuteReader()) {//no transaction needed for a single select!
                while (reader.Read())
                    tracks.Add(new TrackRow {
                        TrackID = (int)(long)reader[0],
                        ArtistID = (int)(long)reader[1],
                        FullTitle = (string)reader[2],
                        LowercaseTitle = (string)reader[3],
                        LookupTimestamp = LookupSimilarityListAge.DbValueTicksToDateTime(reader[4])
                    });

            }
            return tracks.ToArray();
        }
    }
}
