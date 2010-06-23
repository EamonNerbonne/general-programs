using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public class ArtistSetCurrentSimList : AbstractLfmCacheQuery {
		public ArtistSetCurrentSimList(LastFMSQLiteCache lfm)
			: base(lfm) {
			listId = DefineParameter("@listId");
		}
		protected override string CommandText {
			get {
				return @"
UPDATE Artist SET 
	CurrentSimilarArtistList = @listId,
	CurrentSimilarArtistListTimestamp = (select LookupTimestamp from SimilarArtistList where ListID = @listId)
WHERE ArtistID=(select ArtistID from SimilarArtistList where ListID = @listId) 
";
			}
		}


		DbParameter listId;

		public void Execute(SimilarArtistsListId listID) {
			lock (SyncRoot) {
				listId.Value = listID.id;
				CommandObj.ExecuteNonQuery();
			}
		}

	}
}
