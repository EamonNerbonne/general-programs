using System.Collections.Generic;
namespace LastFMspider.LastFMSQLiteBackend {
    public class ArtistRow {
        
        public int ArtistID;
        public string FullArtist;
        public string LowercaseArtist;

    }
    public class RawArtists : AbstractLfmCacheQuery {
        public RawArtists(LastFMSQLiteCache lfm) : base(lfm) { }
        protected override string CommandText {
            get { return @"SELECT ArtistID, FullArtist, LowercaseArtist FROM Artist"; }
        }

        public ArtistRow[] Execute() {
            lock (SyncRoot) {

                var artists = new List<ArtistRow>();
                using (var reader = CommandObj.ExecuteReader()) {//no transaction needed for a single select!
                    while (reader.Read())
                        artists.Add(new ArtistRow {
                            ArtistID = (int)(long)reader[0],
                            FullArtist = (string)reader[1],
                            LowercaseArtist = (string)reader[2]
                        });
                }
                return artists.ToArray();
            }
        }
    }
}
