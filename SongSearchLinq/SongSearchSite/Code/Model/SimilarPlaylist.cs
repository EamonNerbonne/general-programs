namespace SongSearchSite.Code.Model
{
	public class SimilarPlaylist
	{
		public PlaylistEntry[] known { get; set; }
		public string[] unknown { get; set; }
		public int lookups { get; set; }
		public int weblookups { get; set; }
		public int milliseconds { get; set; }
		public int msSimDb { get; set; }
	}
}