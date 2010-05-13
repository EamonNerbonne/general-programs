using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public class TrackSetCurrentSimList : AbstractLfmCacheQuery {
		public TrackSetCurrentSimList(LastFMSQLiteCache lfm)
			: base(lfm) {
			listId = DefineParameter("@listId");
		}
		protected override string CommandText {
			get {
				return @"
UPDATE Track SET CurrentSimilarTrackList = @listId 
WHERE TrackID=(select TrackID from SimilarTrackList where ListID = @listId) 
";
			}
		}


		DbParameter listId;


		public void Execute(SimilarTracksListId ListID) {
			lock (SyncRoot) {
				listId.Value = ListID.Id;
				CommandObj.ExecuteNonQuery();
			}
		}

	}
}
