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

            listID = DefineParameter("@listID");

            lowerTrackB = DefineParameter("@lowerTrackB");
            lowerArtistB = DefineParameter("@lowerArtistB");

        }
        protected override string CommandText {
            get { return @"
INSERT OR REPLACE INTO [SimilarTrack] (ListID, TrackB, Rating) 
SELECT @listID as ListID, B.TrackID, @rating as Rating
FROM Artist BA,Track B,
WHERE BA.LowercaseArtist = @lowerArtistB 
AND BA.ArtistID = B.ArtistID 
AND B.LowercaseTitle = @lowerTrackB
";
            }
        }

        DbParameter listID, lowerTrackB, rating,lowerArtistB;
      

        //TODO fix call
        public void Execute(object o,int listID, SongRef songRefB, double rating) {
            lfmCache.InsertTrack.Execute(songRefB);
            this.rating.Value = rating;
            this.listID.Value = listID;
            this.lowerArtistB.Value = songRefB.Artist.ToLatinLowercase();
            this.lowerTrackB.Value = songRefB.Title.ToLatinLowercase();
            CommandObj.ExecuteNonQuery();
        }

    }
}
