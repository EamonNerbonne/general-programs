using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
    public class UpdateTrackTimestamp:AbstractLfmCacheQuery {
        public UpdateTrackTimestamp(LastFMSQLiteCache lfm)
            : base(lfm) {
            lowerTitle = DefineParameter("@lowerTitle");
            lowerArtist = DefineParameter("@lowerArtist");
            ticks = DefineParameter("@ticks");
        }
        protected override string CommandText {
            get {
                return @"
UPDATE [Track] SET LookupTimestamp = @ticks
WHERE LowercaseTitle = @lowerTitle AND ArtistID = (SELECT ArtistID FROM [Artist] WHERE LowercaseArtist = @lowerArtist)
";
            }
        }
        //note that SQLite Administrator doesn't support numbers outside of int32!  It will display these timestamps as 0 even though they're actually some 64-bit number.
     
        
        DbParameter lowerTitle, lowerArtist, ticks;
    

        public void Execute(SongRef songRef, DateTime dateTime) {
            ticks.Value = dateTime.Ticks;
            lowerArtist.Value = songRef.Artist.ToLowerInvariant();
            lowerTitle.Value = songRef.Title.ToLowerInvariant();
            CommandObj.ExecuteNonQuery();
        }

    }
}
