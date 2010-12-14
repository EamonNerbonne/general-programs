using System;
using System.Linq;

namespace LastFMspider {
	internal static partial class ToolsInternal {

		public static void EnsureLocalFilesInDB(SongTools tools) {
			var LastFmCache = tools.LastFmCache;

			Console.WriteLine("Loading song database...");
			Console.WriteLine("Taking  {0} songs putting artist/title into DB...", tools.SongFilesSearchData.songs.Length);
			SongRef[] songsToDownload = tools.FindByName.Select(group => group.Key).OfType<SongRef>().ToArray();
			LastFmCache.DoInLockedTransaction(() => {
				LastFmCache.DoInLockedTransaction(() => {
					int progressC = 0;
					foreach (SongRef songref in songsToDownload) {
						try {
							LastFmCache.InsertTrack.Execute(songref);
						} catch (Exception e) {
							Console.WriteLine("Exception: {0}", e);
						}//ignore all errors.
						ProgressReport(progressC++, songsToDownload.Length);
					}
				});
			});
		}

		private static void ProgressReport(int current, int count) {
			if (current + 1 == count)
				Console.WriteLine("done.");
			else if ((current + 1) * 100L / count != current * 100L / count)
				Console.Write((current + 1) * 100L / count + "% ");
		}
	}
}
