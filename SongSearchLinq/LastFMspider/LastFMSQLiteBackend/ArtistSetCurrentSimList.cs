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
UPDATE Artist SET CurrentSimilarArtistList = @listId 
WHERE ArtistID=(select ArtistID from SimilarArtistList where ListID = @listId) 
";
            }
        }


		DbParameter listId;


        public void Execute(long ListID) {
            lock (SyncRoot) {
				listId.Value = ListID;
                CommandObj.ExecuteNonQuery();
            }
        }

    }
}
