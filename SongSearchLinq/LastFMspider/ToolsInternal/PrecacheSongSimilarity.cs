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
		public static int PrecacheSongSimilarity(SongTools tools) {
			var LastFmCache = tools.LastFmCache;
			DateTime maxDate = DateTime.UtcNow - TimeSpan.FromDays(365.0);

			Console.WriteLine("Finding songs without similarities");
			var tracksToGo = LastFmCache.TracksWithoutSimilarityList.Execute(300000, maxDate);
#if !DEBUG
			tracksToGo.Shuffle();
#endif
			tracksToGo = tracksToGo.Take(30000).ToArray();
			Console.WriteLine("Looking up similarities for {0} tracks...", tracksToGo.Length);

			using (var toInsert = new BlockingCollection<SongSimilarityList>())
			using (Task<int> inserter = Task.Factory.StartNew(() => {
				int songsCached = 0;
				while (!toInsert.IsAddingCompleted) {
					var nextBatchToInsert = toInsert.GetConsumingEnumerable().Take(100).ToArray();
					LastFmCache.DoInLockedTransaction(() => {
						foreach (var newEntry in nextBatchToInsert) {
							LastFmCache.InsertSimilarityList.Execute(newEntry);
							songsCached++;
						}
					});
				}
				return songsCached;
			})) {
				Parallel.ForEach(tracksToGo, new ParallelOptions { MaxDegreeOfParallelism = 10 }, track => {
					StringBuilder msg = new StringBuilder();
					try {
						string trackStr = track.SongRef.ToString();
						msg.AppendFormat("SimTo:{0,-30}", trackStr.Substring(0, Math.Min(trackStr.Length, 30)));
						TrackSimilarityListInfo info = LastFmCache.LookupSimilarityListInfo.Execute(track.SongRef);
						if (info.LookupTimestamp.HasValue && info.LookupTimestamp.Value > maxDate) {
							msg.AppendFormat("done.");
						} else {
							var newEntry = OldApiClient.Track.GetSimilarTracks(track.SongRef);
							msg.AppendFormat("={0,3} ", newEntry.similartracks.Length);
							if (newEntry.similartracks.Length > 0)
								msg.AppendFormat("{1}: {0}", newEntry.similartracks[0].similarsong.ToString().Substring(0, Math.Min(newEntry.similartracks[0].similarsong.ToString().Length, 30)), newEntry.similartracks[0].similarity);

							toInsert.Add(newEntry);
						}
					} catch (Exception e) {
						try {
							toInsert.Add(SongSimilarityList.CreateErrorList(track.SongRef, 1));//unknown error => code 1
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
