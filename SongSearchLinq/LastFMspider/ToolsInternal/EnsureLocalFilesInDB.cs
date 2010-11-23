using System;
using System.Linq;

namespace LastFMspider {
	internal static partial class ToolsInternal {

		public static void EnsureLocalFilesInDB(SongTools tools) {
			var DB = tools.SongsOnDisk;
			var LastFmCache = tools.LastFmCache;

			Console.WriteLine("Loading song database...");
			if (DB.InvalidDataCount != 0)
				Console.WriteLine("Ignored {0} songs with unknown tags (should be 0).", DB.InvalidDataCount);
			Console.WriteLine("Taking those {0} songs and indexing em by artist/title...", DB.Songs.Length);
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
