using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public class InsertSimilarity : AbstractLfmCacheQuery {
		public InsertSimilarity(LastFMSQLiteCache lfm)
			: base(lfm) {

			rating = DefineParameter("@rating");

			listID = DefineParameter("@listID");

			trackB = DefineParameter("@trackB");

		}
		protected override string CommandText {
			get {
				return @"
INSERT OR REPLACE INTO [SimilarTrack] (ListID, TrackB, Rating) 
VALUES(@listID , @trackB , @rating )
";
			}
		}

		DbParameter listID, trackB, rating;


		public void Execute(SimilarTracksListId listID, SongRef songRefB, double rating) {
			lock (SyncRoot) {
				using (DbTransaction trans = Connection.BeginTransaction()) {

					int trackID = lfmCache.InsertTrack.Execute(songRefB);
					this.rating.Value = rating;
					this.listID.Value = listID.Id;
					this.trackB.Value = trackID;
					CommandObj.ExecuteNonQuery();

					trans.Commit();
				}
			}
		}

	}
}
