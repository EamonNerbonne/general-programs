
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public class InsertArtistTopTrack : AbstractLfmCacheQuery {
		public InsertArtistTopTrack(LastFMSQLiteCache lfm)
			: base(lfm) {

			reach = DefineParameter("@reach");

			listID = DefineParameter("@listID");

			artistID = DefineParameter("@artistID");

			lowerTitleB = DefineParameter("@lowerTitleB");
			fullTitleB = DefineParameter("@fullTitleB");

		}
		protected override string CommandText {
			get {
				return @"
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



		public void Execute(TopTracksListId listIdArg, ArtistId artistIdArg, string trackTitle, long reach) {
			lock (SyncRoot) {
				this.reach.Value = reach;
				this.listID.Value = listIdArg.Id;
				lowerTitleB.Value = trackTitle.ToLatinLowercase();
				fullTitleB.Value = trackTitle;
				this.artistID.Value = artistIdArg.Id;

				CommandObj.ExecuteNonQuery();
			}
		}

	}



}
