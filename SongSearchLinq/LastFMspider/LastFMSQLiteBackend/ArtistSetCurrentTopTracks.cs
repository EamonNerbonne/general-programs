using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public class ArtistSetCurrentTopTracks : AbstractLfmCacheQuery {
		public ArtistSetCurrentTopTracks(LastFMSQLiteCache lfm)
			: base(lfm) {
			listIdQueryParam = DefineParameter("@listId");
		}
		protected override string CommandText {
			get {
				return @"
UPDATE Artist SET 
	CurrentTopTracksList = @listId,
	CurrentTopTracksListTimestamp = (select LookupTimestamp from TopTracksList where ListID = @listId)
WHERE ArtistID=(select ArtistID from TopTracksList where ListID = @listId) 
";
			}
		}

		readonly DbParameter listIdQueryParam;

		public void Execute(TopTracksListId listIdArg) {
			lock (SyncRoot) {
				listIdQueryParam.Value = listIdArg.id;
				CommandObj.ExecuteNonQuery();
			}
		}
	}
}
