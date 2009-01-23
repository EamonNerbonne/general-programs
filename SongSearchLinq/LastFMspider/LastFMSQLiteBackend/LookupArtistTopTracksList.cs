using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.SQLite;

namespace LastFMspider.LastFMSQLiteBackend
{
    public struct ArtistTopTrack
    {
        public string Track;
        public long Reach;
    }
    public class ArtistTopTracksList
    {
        public DateTime LookupTimestamp;
        public string Artist;
        public ArtistTopTrack[] TopTracks;
        public int? StatusCode;
    }

    public class LookupArtistTopTracksList : AbstractLfmCacheQuery
    {
        public LookupArtistTopTracksList(LastFMSQLiteCache lfmCache)
            : base(lfmCache) {
            lowerArtist = DefineParameter("@lowerArtist");
            ticks = DefineParameter("@ticks");
        }
        protected override string CommandText {
            get {
                return @"
SELECT B.FullTitle, T.Reach
FROM Artist A 
join TopTracksList L on A.ArtistID = L.ArtistID
join TopTracks T on L.ListID = T.ListID
join Track B on T.TrackID = B.TrackID
WHERE A.LowercaseArtist = @lowerArtist
AND L.LookupTimestamp = @ticks
";
            }
        }
        DbParameter lowerArtist,ticks;

        public ArtistTopTracksList Execute(string artist) {
            lock (SyncRoot) {

                using (var trans = Connection.BeginTransaction()) {
                    ArtistQueryInfo info = lfmCache.LookupArtistTopTracksListAge.Execute(artist);
                    if (info.IsAlternateOf.HasValue)
                        return Execute(lfmCache.LookupArtist.Execute(info.IsAlternateOf.Value));
                    if (!info.LookupTimestamp.HasValue)
                        return null;
                    DateTime age = info.LookupTimestamp.Value;

                    lowerArtist.Value = artist.ToLatinLowercase();
                    ticks.Value = age.Ticks;//we want the newest one!

                    List<ArtistTopTrack> toptracks = new List<ArtistTopTrack>();
                    using (var reader = CommandObj.ExecuteReader()) {
                        while (reader.Read())
                            toptracks.Add(new ArtistTopTrack {
                                Track = (string)reader[0],
                                Reach = (long)reader[1],
                            });
                    }
                    var retval = new ArtistTopTracksList {
                        Artist = artist,
                        TopTracks = toptracks.ToArray(),
                        LookupTimestamp = age,
                        StatusCode = info.StatusCode,
                    };
                    return retval;
                }
            }
        }


    }
}
