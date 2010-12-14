//#define NOPRECACHE
using System;
using System.Collections.Generic;
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
			public int LookupsDone;
			public int LookupsWebTotal;
		}
		static bool NeverAbort(int i) { return false; }

		public static SimilarPlaylistResults ProcessPlaylist(SongTools tools, Func<SongRef, SongMatch> fuzzySearch, IEnumerable<SongRef> seedSongs,
			int MaxSuggestionLookupCount = 100, int SuggestionCountTarget = 50, Func<int, bool> shouldAbort = null
			) {

			//OK, so we now have the playlist in the var "playlist" with knowns in "known" except for the unknowns, which are in "unknown" as far as possible.
			shouldAbort = shouldAbort ?? NeverAbort;
			var simSongDb = tools.LastFmCache;
			return simSongDb.DoInTransaction(() => {
				SimilarPlaylistResults res = new SimilarPlaylistResults();

				var playlistSongs = seedSongs.Select(simSongDb.LookupTrackID.Execute).Where(trackid => trackid.HasValue).Distinct().Reverse().ToArray();
				Dictionary<TrackId, HashSet<TrackId>> playlistSongRefs = playlistSongs.ToDictionary(sr => sr, sr => new HashSet<TrackId>());
				HashSet<SongRef> seedSongSet = new HashSet<SongRef>(); //used to ensure fuzzy matches don't readd existing song.

				SongWithCostCache songCostCache = new SongWithCostCache();
				IHeap<SongWithCost> songCosts = Heap.Factory<SongWithCost>().Create((sc, index) => { sc.index = index; });

#if !NOPRECACHE
				Dictionary<TrackId, Task<TrackSimilarityListInfo>> bgLookupCache = new Dictionary<TrackId, Task<TrackSimilarityListInfo>>();
				Action<TrackId> precache = trackid => {
					if (!bgLookupCache.ContainsKey(trackid)) {
						bgLookupCache[trackid] = Task.Factory.StartNew(() => simSongDb.LookupSimilarityListInfo.Execute(trackid));
						res.LookupsDone++;
					}
				};
#endif

				foreach (var songcost in playlistSongs.Select(songCostCache.Lookup)) {
					songcost.cost = 0.0;
					songcost.graphDist = 0;
					songcost.basedOn.Add(songcost.trackid);
#if !NOPRECACHE
					precache(songcost.trackid);
#endif
					songCosts.Add(songcost);
				}

				int lastPercent = 0;
				while (!shouldAbort(res.knownTracks.Count) && res.similarList.Count < MaxSuggestionLookupCount && res.knownTracks.Count < SuggestionCountTarget) {
#if !NOPRECACHE
					foreach (var trackid in songCosts.ElementsInRoughOrder.Select(songwithcost => songwithcost.trackid).Take(2))
						precache(trackid);
#endif

					SongWithCost currentSong;
					if (!songCosts.RemoveTop(out currentSong)) break;
					currentSong.index = -1;
					if (!playlistSongRefs.ContainsKey(currentSong.trackid)) {
						SongRef currentSongRef = simSongDb.LookupTrack.Execute(currentSong.trackid);
						if (currentSongRef != null) { //null essentially means DB corruption: reference to a trackId that doesn't exist.  We ignore that trackid.
							res.similarList.Add(currentSong);
							if (tools.FindByName[currentSongRef].Any())
								res.knownTracks.Add((
									from songcandidate in tools.FindByName[currentSongRef]
									orderby SongMatch.AbsoluteSongCost(songcandidate)
									select songcandidate
								).First());
							else {
								SongMatch bestRoughMatch = fuzzySearch(currentSongRef);
								if (bestRoughMatch.GoodEnough && !seedSongSet.Contains(SongRef.Create(bestRoughMatch.Song)))
									res.knownTracks.Add(bestRoughMatch.Song);
								else
									res.unknownTracks.Add(currentSongRef);
							}
						}
					}

#if !NOPRECACHE
					var nextSimlist = bgLookupCache[currentSong.trackid].Result;
#else
					var nextSimlist = simSongDb.LookupSimilarityListInfo.Execute(currentSong.trackid);
					res.LookupsDone++;
#endif

					int simRank = 0;
					foreach (var similarTrack in nextSimlist.SimilarTracks) {
						SongWithCost similarSong = songCostCache.Lookup(similarTrack.OtherId);
						foreach (var baseSong in currentSong.basedOn)
							similarSong.basedOn.Add(baseSong);
						double directCost = (simRank + similarSong.basedOn.Select(baseSong => playlistSongRefs[baseSong].Count + 50).Sum()) / (similarSong.basedOn.Count * Math.Sqrt(similarSong.basedOn.Count));
						simRank++;
						if (similarSong.index == -1 && similarSong.cost < double.PositiveInfinity) //not in heap but with cost: we've already been fully processed!
							continue;
						else if (similarSong.index == -1) { //not in the heap but without cost: not processed at all!
							similarSong.cost = directCost;
							songCosts.Add(similarSong);
						} else { // still in heap
							songCosts.Delete(similarSong.index);
							similarSong.cost = directCost;
							similarSong.index = -1;
							songCosts.Add(similarSong);
						}
					}
					foreach (var srcSong in currentSong.basedOn) {
						var dependantSongs = playlistSongRefs[srcSong];
						foreach (var simSong in nextSimlist.SimilarTracks)
							dependantSongs.Add(simSong.OtherId);
					}
					int newPercent = Math.Max((res.similarList.Count * 100) / MaxSuggestionLookupCount, (res.knownTracks.Count * 100) / SuggestionCountTarget);
					if (newPercent > lastPercent) {
						lastPercent = newPercent;
						string msg = "[" + songCosts.Count + ";" + newPercent + "%]";
						Console.Write(msg.PadRight(16, ' '));
					}
				}

				tools.SimilarSongs.RefreshCacheIfNeeded(bgLookupCache.Keys.ToArray());

				Console.WriteLine("{0} similar tracks generated, of which {1} found locally.", res.similarList.Count, res.knownTracks.Count);
				res.LookupsWebTotal = LookupSimilarTracksHelper.WebLookupsSoFar();
				return res;
			});
		}
	}
}
