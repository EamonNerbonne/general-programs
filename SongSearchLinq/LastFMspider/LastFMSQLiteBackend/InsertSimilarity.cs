using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
    public class InsertSimilarity:AbstractLfmCacheQuery {
        public InsertSimilarity(LastFMSQLiteCache lfm)
            : base(lfm) {

            rating = DefineParameter("@rating");

            lowerArtistA = DefineParameter("@lowerArtistA");

            lowerTitleA = DefineParameter("@lowerTitleA");

            lowerArtistB = DefineParameter("@lowerArtistB");

            lowerTitleB = DefineParameter("@lowerTitleB");
        }
        protected override string CommandText {
            get { return @"
INSERT OR REPLACE INTO [SimilarTrack] (TrackA, TrackB, Rating) 
SELECT A.TrackID, B.TrackID, (@rating) AS Rating
FROM Track A, Track B, Artist AsArtist, Artist BsArtist WHERE A.ArtistID = AsArtist.ArtistID AND B.ArtistID == BsArtist.ArtistID
  AND AsArtist.LowercaseArtist = @lowerArtistA AND A.LowercaseTitle == @lowerTitleA 
  AND BsArtist.LowercaseArtist = @lowerArtistB AND B.LowercaseTitle == @lowerTitleB
";}
        }

        DbParameter lowerTitleA, lowerTitleB, lowerArtistA, lowerArtistB, rating;
      


        public void Execute(SongRef songRefA, SongRef songRefB, double rating) {
            this.rating.Value = rating;
            lowerArtistA.Value = songRefA.Artist.ToLowerInvariant();
            lowerTitleA.Value = songRefA.Title.ToLowerInvariant();
            lowerArtistB.Value = songRefB.Artist.ToLowerInvariant();
            lowerTitleB.Value = songRefB.Title.ToLowerInvariant();
            CommandObj.ExecuteNonQuery();
        }

    }
}
