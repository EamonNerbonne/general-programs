using System;
using System.Linq;

namespace LastFMspider {
	internal static partial class ToolsInternal {

		public static void EnsureLocalFilesInDB(SongTools tools) {
			var LastFmCache = tools.LastFmCache;

			Console.WriteLine("Loading song database...");
			Console.WriteLine("Taking  {0} songs and indexing em by artist/title...", tools.SongFilesSearchData.songs.Length);
			SongRef[] songsToDownload = tools.FindByName.Select(group => group.Key).ToArray();
			LastFmCache.DoInLockedTransaction(() => {
				foreach (SongRef songref in songsToDownload) {
					try {
						LastFmCache.InsertTrack.Execute(songref);
					} catch (Exception e) {
						Console.WriteLine("Exception: {0}", e);
					}//ignore all errors.
				}
			});
		}
	}
}
