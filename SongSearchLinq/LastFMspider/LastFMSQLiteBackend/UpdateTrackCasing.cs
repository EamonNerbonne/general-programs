using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
    public class UpdateTrackCasing: AbstractLfmCacheQuery {
        public UpdateTrackCasing(LastFMSQLiteCache lfm)
            : base(lfm) {
            lowerTitle = DefineParameter("@lowerTitle");
            lowerArtist = DefineParameter("@lowerArtist");
            fullTitle = DefineParameter("@fullTitle");
            fullArtist = DefineParameter("@fullArtist");
        }
        protected override string CommandText {
            get {
                return @"
UPDATE Artist SET FullArtist = @fullArtist WHERE LowercaseArtist=@lowerArtist;
UPDATE Track SET FullTitle = @fullTitle WHERE LowercaseTitle=@lowerTitle AND ArtistID = (SELECT ArtistID FROM Artist WHERE LowercaseArtist = @lowerArtist)
";
            }
        }
        

        DbParameter lowerTitle, lowerArtist, fullTitle,fullArtist;


        public void Execute(SongRef songRef) {
            lock (SyncRoot) {

                lowerArtist.Value = songRef.Artist.ToLatinLowercase();
                lowerTitle.Value = songRef.Title.ToLatinLowercase();
                fullTitle.Value = songRef.Title;
                fullArtist.Value = songRef.Artist;
                CommandObj.ExecuteNonQuery();
            }
        }

    }
}
