using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.SQLite;
using System.Data;

namespace LastFMspider.LastFMSQLiteBackend {
	internal class ConvertOldSimList : AbstractLfmCacheQuery {
		public ConvertOldSimList(LastFMSQLiteCache lfmCache)
			: base(lfmCache) {
				listBlob = DefineParameter("@listBlob",DbType.Binary);
				listId = DefineParameter("@listId");
		}
		protected override string CommandText {
			get {
				return @"
UPDATE TopTracksList SET TopTracks = @listBlob WHERE ListID=@listId;
DELETE FROM TopTracks WHERE ListID=@listId;
";
			}
		}
		DbParameter listBlob;
		DbParameter listId;

		public void Execute(ReachList<TrackId, TrackId.Factory> newlist, TopTracksListId listId) {
			if (!listId.HasValue)
				throw new ArgumentException("List Id must be set to use this operation");
			lock (SyncRoot) {
				using (var trans = Connection.BeginTransaction()) {
					this.listBlob.Value = newlist.encodedSims.Length == 0 ? null : newlist.encodedSims;
					this.listId.Value = listId.id;
					CommandObj.ExecuteNonQuery();
					trans.Commit();
				}
			}
		}
	}
}
