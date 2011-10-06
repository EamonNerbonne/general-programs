//#define NOPRECACHE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EmnExtensions.Collections;
using LastFMspider.LastFMSQLiteBackend;
using SongDataLib;

namespace LastFMspider {
	public static partial class FindSimilarPlaylist {

		public class SimilarPlaylistResults {
			public readonly List<SongFileData> knownTracks = new List<SongFileData>();
			public readonly List<SongRef> unknownTracks = new List<SongRef>();
			public readonly List<SongWithCost> similarList = new List<SongWithCost>();
			public int ResultsCount() { return knownTracks.Count + unknownTracks.Count; }
			public int LookupsDone;
			public int LookupsWebTotal;
		}
		static bool NeverAbort(int i) { return false; }

		public static SimilarPlaylistResults ProcessPlaylist(SongTools tools, Func<SongRef, SongFileData> fuzzySearch, IEnumerable<SongFileData> seedSongs, IEnumerable<SongRef> unknownSeedSongs, out double simLookupMs,
	int MaxSuggestionLookupCount = 100, int SuggestionCountTarget = 50, Func<int, bool> shouldAbort = null
	) {
			var simSongDb = tools.LastFmCache;
			foreach (var songdata in seedSongs) {
				if (songdata.TrackID == 0) {
					SongRef songref = songdata.PossibleSongs.FirstOrDefault();
					songdata.TrackID = songref == null ? uint.MaxValue : simSongDb.LookupTrackID.Execute(songref).Id;
				}
			}
			IEnumerable<TrackId> trackIds = seedSongs.Select(songdata => new TrackId(songdata.TrackID)).Where(id => id.Id != uint.MaxValue && id.HasValue)
				.Concat(unknownSeedSongs.Select(simSongDb.LookupTrackID.Execute))
				;
			return
				ProcessPlaylist(tools, fuzzySearch, trackIds, new HashSet<SongFileData>(seedSongs),
				out simLookupMs, MaxSuggestionLookupCount, SuggestionCountTarget, shouldAbort);
		}

		public static SimilarPlaylistResults ProcessPlaylist(SongTools tools, Func<SongRef, SongFileData> fuzzySearch, IEnumerable<TrackId> seedSongs, HashSet<SongFileData> seedSongSet, out double simLookupMs,
			int MaxSuggestionLookupCount = 100, int SuggestionCountTarget = 50, Func<int, bool> shouldAbort = null
			) {

			//OK, so we now have the playlist in the var "playlist" with knowns in "known" except for the unknowns, which are in "unknown" as far as possible.
			shouldAbort = shouldAbort ?? NeverAbort;
			var simSongDb = tools.LastFmCache;
			var lookupRes = simSongDb.DoInTransaction(() => {
				SimilarPlaylistResults res = new SimilarPlaylistResults();

				var playlistSongs = seedSongs.Distinct().Reverse().ToArray();

				SongWithCostCache songCostCache = new SongWithCostCache();
				IHeap<SongWithCost> songCosts = Heap.Factory<SongWithCost>().Create((sc, index) => { sc.index = index; });

#if !NOPRECACHE
				Dictionary<TrackId, Task<TrackSimilarityListInfo>> bgLookupCache = new Dictionary<TrackId, Task<TrackSimilarityListInfo>>();
				object sync = new object();
				Stopwatch sw = new Stopwatch();
				Action<TrackId> precache = trackid => {
					if (!bgLookupCache.ContainsKey(trackid)) {
						bgLookupCache[trackid] = Task.Factory.StartNew(() => {
							var retval = simSongDb.LookupSimilarityListInfo.Execute(trackid);
							return retval;
						});
						res.LookupsDone++;
					}
				};
#endif

				foreach (var songcost in playlistSongs.Select(songCostCache.Lookup)) {
					songcost.cost = 0.0;
					songcost.graphDist = 0;
					songcost.basedOn.Add(songcost);
#if !NOPRECACHE
					precache(songcost.trackid);
#endif
					songCosts.Add(songcost);
				}

				int lastPercent = 0;
				while (!shouldAbort(res.knownTracks.Count) && res.ResultsCount() < MaxSuggestionLookupCount && res.knownTracks.Count < SuggestionCountTarget) {
#if !NOPRECACHE
					foreach (var trackid in songCosts.ElementsInRoughOrder.Select(songwithcost => songwithcost.trackid).Take(3))
						precache(trackid);
#endif

					SongWithCost currentSong;
					if (!songCosts.RemoveTop(out currentSong)) break;
					currentSong.index = -1;
					if (currentSong.graphDist != 0) {
						res.similarList.Add(currentSong);
						object songOrRef = GetSong(simSongDb, tools, fuzzySearch, currentSong.trackid);
						if (songOrRef != null) {
							if (songOrRef is SongRef)
								res.unknownTracks.Add((SongRef)songOrRef);
							else if (!seedSongSet.Contains((SongFileData)songOrRef))
								res.knownTracks.Add((SongFileData)songOrRef);
						}
					}

#if !NOPRECACHE
					var currentSimlist = bgLookupCache[currentSong.trackid].Result;
#else
					var nextSimlist = simSongDb.LookupSimilarityListInfo.Execute(currentSong.trackid);
					res.LookupsDone++;
#endif

					int simRank = 0;
					sw.Start();
					foreach (var similarTrack in currentSimlist.SimilarTracks) {
						SongWithCost similarSong = songCostCache.Lookup(similarTrack.OtherId);
						foreach (var srcSong in currentSong.basedOn) {
							similarSong.basedOn.Add(srcSong);
							srcSong.dependants.Add(similarSong);
						}
						double directCost = (simRank + similarSong.basedOn.Select(srcSong => srcSong.dependants.Count + 50).Sum()) / (similarSong.basedOn.Count * Math.Sqrt(similarSong.basedOn.Count));
						simRank++;
						if (similarSong.index == -1 && similarSong.cost < double.PositiveInfinity) //not in heap but with cost: we've already been fully processed!
							continue;
						else if (similarSong.index == -1) { //not in the heap but without cost: not processed at all!
							similarSong.cost = directCost;
							similarSong.graphDist = currentSong.graphDist + 1;
							songCosts.Add(similarSong);
						} else { // still in heap
							songCosts.Delete(similarSong.index);
							similarSong.cost = directCost;
							similarSong.index = -1;
							similarSong.graphDist = Math.Min(similarSong.graphDist, currentSong.graphDist + 1);
							songCosts.Add(similarSong);
						}
					}
					sw.Stop();
					int newPercent = Math.Max((res.ResultsCount() * 100) / MaxSuggestionLookupCount, (res.knownTracks.Count * 100) / SuggestionCountTarget);
					if (newPercent > lastPercent) {
						lastPercent = newPercent;
						string msg = "[" + songCosts.Count + ";" + newPercent + "%]";
						Console.Write(msg.PadRight(16, ' '));
					}
				}
				if(bgLookupCache.Any())
					Task.Factory.ContinueWhenAll(bgLookupCache.Values.ToArray(), simListTasks => tools.SimilarSongs.RefreshCacheIfNeeded(simListTasks.Select(task => task.Result).ToArray()));
				// bgLookupCache.Where(kvp => !LookupSimilarTracksHelper.IsFresh(kvp.Value.Result)).ToArray());	

				Console.WriteLine("{0} similar tracks generated, of which {1} found locally.", res.ResultsCount(), res.knownTracks.Count);
				res.LookupsWebTotal = LookupSimilarTracksHelper.WebLookupsSoFar();
				return new { sw.Elapsed.TotalMilliseconds, res };
			});
			simLookupMs = lookupRes.TotalMilliseconds;
			return lookupRes.res;
		}

		//static readonly ConcurrentDictionary<TrackId, object> cache = new ConcurrentDictionary<TrackId, object>();

		static object GetSong(LastFMSQLiteCache simSongDb, SongTools tools, Func<SongRef, SongFileData> fuzzySearch, TrackId trackId) {
//			return cache.GetOrAdd(trackId, id => {
				var currentSongRef = simSongDb.LookupTrack.Execute(trackId);
				return currentSongRef == null ? null
						: (tools.FindByName[currentSongRef].Any() ? (
								from songcandidate in tools.FindByName[currentSongRef]
								orderby SongMatch.AbsoluteSongCost(songcandidate)
								select songcandidate
							  ).First()
						: (fuzzySearch(currentSongRef) ?? (object)currentSongRef));
	//		});
		}
	}
}
