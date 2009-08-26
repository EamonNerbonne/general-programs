using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
    public class ArtistSetCurrentTopTracks : AbstractLfmCacheQuery {
		public ArtistSetCurrentTopTracks(LastFMSQLiteCache lfm)
            : base(lfm) {
            listId = DefineParameter("@listId");
        }
        protected override string CommandText {
            get {
                return @"
UPDATE Artist SET CurrentTopTracksList = @listId 
WHERE ArtistID=(select ArtistID from TopTracksList where ListID = @listId) 
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
