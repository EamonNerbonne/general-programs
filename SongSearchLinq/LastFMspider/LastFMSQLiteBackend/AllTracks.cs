using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LastFMspider.LastFMSQLiteBackend {
    public class CachedTrack {
        public int ID;
        public SongRef SongRef;
    }
    public class AllTracks : AbstractLfmCacheQuery {
        public AllTracks(LastFMSQLiteCache lfm) : base(lfm) { }

        protected override string CommandText {
            get { return @"
SELECT T.TrackID, A.FullArtist, T.FullTitle
FROM Artist A
INNER JOIN Track T ON A.ArtistID = T.ArtistID
"; }
        }

        public CachedTrack[] Execute() {
            List<CachedTrack> tracks = new List<CachedTrack>();
            using (var reader = CommandObj.ExecuteReader()) {//no transaction needed for a single select!
                while (reader.Read())
                    tracks.Add(new CachedTrack {
                        ID = (int)(long)reader[0],
                        SongRef = SongRef.Create((string)reader[1], (string)reader[2]),
                    });

            }
            return tracks.ToArray();
        }
    }
}
