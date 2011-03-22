using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmnExtensions.Algorithms;
using LastFMspider.LastFMSQLiteBackend;
using LastFMspider.OldApi;

namespace LastFMspider {
	internal static partial class ToolsInternal {
		public static int PrecacheArtistTopTracks(SongTools tools) {
			var LastFmCache = tools.LastFmCache;

			DateTime maxDate = DateTime.UtcNow - TimeSpan.FromDays(365.0);

			Console.WriteLine("Finding artists without toptracks");
			var artistsToGo = LastFmCache.ArtistsWithoutTopTracksList.Execute(1000000, maxDate);
#if !DEBUG
			artistsToGo.Shuffle();
#endif
			artistsToGo = artistsToGo.Take(100000).ToArray();
			Console.WriteLine("Looking up top-tracks for {0} artists...", artistsToGo.Length);

			using (var toInsert = new BlockingCollection<ArtistTopTracksList>())
			using (Task<int> inserter = Task.Factory.StartNew(() => {
				int artistsCached = 0;
				while (!toInsert.IsAddingCompleted) {
					var nextBatchToInsert = toInsert.GetConsumingEnumerable().Take(100).ToArray();
					LastFmCache.DoInLockedTransaction(() => {
						foreach (var newEntry in nextBatchToInsert) {
							LastFmCache.InsertArtistTopTracksList.Execute(newEntry);
							artistsCached++;
						}
					});
				}
				return artistsCached;
			})) {

				Parallel.ForEach(artistsToGo, new ParallelOptions { MaxDegreeOfParallelism = 10 }, artist => {
					StringBuilder msg = new StringBuilder();

					try {
						msg.AppendFormat("TopOf:{0,-30}", artist.ArtistName.Substring(0, Math.Min(artist.ArtistName.Length, 30)));
						ArtistTopTracksListInfo info = LastFmCache.LookupArtistTopTracksListAge.Execute(artist.ArtistName);
						if ((info.LookupTimestamp.HasValue && info.LookupTimestamp.Value > maxDate) || info.ArtistInfo.IsAlternateOf.HasValue) {
							msg.AppendFormat("done.");
						} else {
							var newEntry = OldApiClient.Artist.GetTopTracks(artist.ArtistName);
							msg.AppendFormat("={0,3} ", newEntry.TopTracks.Length);
							if (newEntry.TopTracks.Length > 0)
								msg.AppendFormat("{1}: {0}", newEntry.TopTracks[0].Track.Substring(0, Math.Min(newEntry.TopTracks[0].Track.Length, 30)), newEntry.TopTracks[0].Reach);

							if (artist.ArtistName.ToLatinLowercase() != newEntry.Artist.ToLatinLowercase())
								LastFmCache.SetArtistAlternate.Execute(artist.ArtistName, newEntry.Artist);

							toInsert.Add(newEntry);
						}
					} catch (Exception e) {
						try {
							toInsert.Add(ArtistTopTracksList.CreateErrorList(artist.ArtistName, 1));
						} catch (Exception ee) { Console.WriteLine(ee.ToString()); }
						msg.AppendFormat("\n{0}\n", e);
					} finally {
						Console.WriteLine(msg);
					}
				});
				toInsert.CompleteAdding();
				return inserter.Result;
			}
		}
	}
}
