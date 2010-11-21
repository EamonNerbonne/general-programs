﻿using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public class TrackSetCurrentSimList : AbstractLfmCacheQuery {
		public TrackSetCurrentSimList(LastFMSQLiteCache lfm)
			: base(lfm) {
			listId = DefineParameter("@listId");
		}
		protected override string CommandText {
			get {
				return @"
UPDATE Track SET 
	CurrentSimilarTrackList = @listId,
	CurrentSimilarTrackListTimestamp = (select LookupTimestamp from SimilarTrackList where ListID = @listId) 
WHERE TrackID=(select TrackID from SimilarTrackList where ListID = @listId) 
";
			}
		}

		readonly DbParameter listId;

		public void Execute(SimilarTracksListId ListID) {
			lock (SyncRoot) {
				listId.Value = ListID.id;
				CommandObj.ExecuteNonQuery();
			}
		}

	}
}
