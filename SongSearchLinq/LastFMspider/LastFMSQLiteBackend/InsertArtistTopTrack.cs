
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend
{
    public class InsertArtistTopTrack:AbstractLfmCacheQuery {
        public InsertArtistTopTrack(LastFMSQLiteCache lfm)
            : base(lfm) {

            reach = DefineParameter("@reach");

            listID = DefineParameter("@listID");

            artistID = DefineParameter("@artistID");

            lowerTitleB = DefineParameter("@lowerTitleB");
            fullTitleB = DefineParameter("@fullTitleB");

        }
        protected override string CommandText {
            get { return @"
INSERT OR IGNORE INTO [Track] (ArtistID, FullTitle, LowercaseTitle)
VALUES (@artistID, @fullTitleB, @lowerTitleB);

INSERT OR REPLACE INTO [TopTracks] (ListID, TrackID, Reach) 
SELECT @listID as ListID, B.TrackID, @reach as Reach
FROM Track B
WHERE B.ArtistID = @artistID
AND B.LowercaseTitle= @lowerTitleB
";
            }
        }

        DbParameter listID, lowerTitleB, fullTitleB, reach, artistID;
      


        public void Execute(int listID, int artistID, string trackTitle, long reach) {
            this.reach.Value = reach;
            this.listID.Value = (long)listID;
            lowerTitleB.Value = trackTitle.ToLowerInvariant();
            fullTitleB.Value = trackTitle;
            this.artistID.Value = artistID;

            CommandObj.ExecuteNonQuery();
        }

    }


    
}
