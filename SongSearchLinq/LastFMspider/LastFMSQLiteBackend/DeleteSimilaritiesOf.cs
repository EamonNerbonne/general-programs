using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
    public class DeleteSimilaritiesOf :AbstractLfmCacheQuery{
        public DeleteSimilaritiesOf(LastFMSQLiteCache lfm)
            : base(lfm) {
            lowerArtist = DefineParameter("@lowerArtist");

            lowerTitle = DefineParameter("@lowerTitle");
 
        }
 
        DbParameter lowerArtist, lowerTitle;



        public void Execute(SongRef songref) {
            lowerArtist.Value = songref.Artist.ToLowerInvariant();
            lowerTitle.Value = songref.Title.ToLowerInvariant();
            CommandObj.ExecuteNonQuery();

        }


        protected override string CommandText {
            get {
                return @"
DELETE FROM SimilarTrack
WHERE TrackA = (
  SELECT T.TrackID
  FROM Artist A,Track T
  WHERE A.LowercaseArtist= @lowerArtist
  AND A.ArtistID = T.ArtistID
  AND T.LowercaseTitle == @lowerTitle
);
UPDATE OR IGNORE Track
SET LookupTimestamp=NULL
WHERE ArtistID=
  (SELECT A.ArtistID
   FROM Artist A
   WHERE A.LowercaseArtist = @lowerArtist)
AND LowercaseTitle = @lowerTitle
";
            }
        }
    }
}
