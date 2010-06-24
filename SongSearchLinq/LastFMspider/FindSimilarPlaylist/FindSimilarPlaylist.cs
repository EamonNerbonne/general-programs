using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using EmnExtensions.Collections;
using LastFMspider;
using System.Threading;

namespace LastFMspider {
	public static partial class FindSimilarPlaylist {

		public class SimilarPlaylistResults {
			public readonly List<SongData> knownTracks = new List<SongData>();
			public readonly List<SongRef> unknownTracks = new List<SongRef>();
			public readonly List<SongWithCost> similarList = new List<SongWithCost>();
		}
		static bool NeverAbort(int i) { return false; }

		public static SimilarPlaylistResults ProcessPlaylist(LastFmTools tools, Func<SongRef, SongMatch> fuzzySearch, List<SongData> known, List<SongRef> unknown,
			int MaxSuggestionLookupCount = 100, int SuggestionCountTarget = 50, Func<int,bool> shouldAbort = null
			) {
			//OK, so we now have the playlist in the var "playlist" with knowns in "known" except for the unknowns, which are in "unknown" as far as possible.
			shouldAbort = shouldAbort ?? NeverAbort;
			var playlistSongRefs = new HashSet<SongRef>(known.Select(sd => SongRef.Create(sd)).Where(sr => sr != null).Cast<SongRef>().Concat(unknown));
			SimilarPlaylistResults res = new SimilarPlaylistResults();

			SongWithCostCache songCostCache = new SongWithCostCache();
			Heap<SongWithCost> songCosts = new Heap<SongWithCost>((sc, index) => { sc.index = index; });

			HashSet<SongRef> lookupQueue = new HashSet<SongRef>();
			Dictionary<SongRef, SongSimilarityList> lookupCache = new Dictionary<SongRef, SongSimilarityList>();
			Queue<SongRef> lookupCacheOrder = new Queue<SongRef>();
			HashSet<SongRef> lookupsDeleted = new HashSet<SongRef>();
			Dictionary<SongRef, int> usages = playlistSongRefs.ToDictionary(s => s, s => 0);

			object ignore = tools.SimilarSongs;//ensure similarsongs loaded.
			object sync = new object();
			bool done = false;
			Random rand = new Random();

			ThreadStart bgLookup = () => {
				bool tDone;
				lock (sync) tDone = done;
				while (!tDone) {
					SongRef nextToLookup;
					SongSimilarityList simList = null;
					lock (sync) {
						nextToLookup =
							songCosts.ElementsInRoughOrder
							.Select(sc => sc.songref)
							.Where(songref => !lookupQueue.Contains(songref))
							.FirstOrDefault();
						if (nextToLookup != null)
							lookupQueue.Add(nextToLookup);
					}
					if (nextToLookup != null) {
						simList = tools.SimilarSongs.LookupMaybe(nextToLookup);
						lock (sync) {
							lookupCache[nextToLookup] = simList;
							lookupCacheOrder.Enqueue(nextToLookup);
							while (lookupCacheOrder.Count > 10000) {
								SongRef toRemove = lookupCacheOrder.Dequeue();
								lookupCache.Remove(lookupCacheOrder.Dequeue());
								lookupsDeleted.Add(toRemove);
							}
							//todo:notify
						}
					} else {
						Thread.Sleep(100);
					}

					lock (sync) tDone = done;
				}
			};



			Func<SongRef,int, SongSimilarityList> lookupParallel = (songref,curcount) => {
				SongSimilarityList retval;
				bool notInQueue;
				bool alreadyDeleted;
				while (true) {

					lock (sync) {
						if (lookupCache.TryGetValue(songref, out retval))
							return retval; //easy case
						alreadyDeleted = lookupsDeleted.Contains(songref);
						notInQueue = !lookupQueue.Contains(songref);
						if (notInQueue)
							lookupQueue.Add(songref);
					}
					if (alreadyDeleted)
						return tools.SimilarSongs.LookupMaybe(songref);
					if (notInQueue) {
						retval = tools.SimilarSongs.LookupMaybe(songref);
						lock (sync) lookupsDeleted.Add(songref);
					}
					//OK, so song is in queue, not in cache but not deleted from cache: song must be in flight: we wait and then try again.
					Thread.Sleep(10);
					if (shouldAbort(curcount)) return null;
				}
			};


			foreach (var songcost in playlistSongRefs.Select(songref => songCostCache.Lookup(songref))) {
				songcost.cost = 0.0;
				songcost.basedOn.Add(songcost.songref);
				songCosts.Add(songcost);
			}

			for (int i = 0; i < 3; i++) { new Thread(bgLookup) { Priority = ThreadPriority.BelowNormal }.Start(); }

			int lastPercent = 0;
			try {
				while (!shouldAbort(res.similarList.Count) && res.similarList.Count < MaxSuggestionLookupCount && res.knownTracks.Count < SuggestionCountTarget) {
					SongWithCost currentSong;
					lock (sync)
						if (!songCosts.RemoveTop(out currentSong))
							break;
					if (!playlistSongRefs.Contains(currentSong.songref)) {
						res.similarList.Add(currentSong);
						if (tools.Lookup.dataByRef.ContainsKey(currentSong.songref))
							res.knownTracks.Add((
								from songcandidate in tools.Lookup.dataByRef[currentSong.songref]
								orderby SongMatch.AbsoluteSongCost(songcandidate)
								select songcandidate
							).First());
						else {
							SongMatch bestRoughMatch = fuzzySearch(currentSong.songref);
							if (bestRoughMatch.GoodEnough)
								res.knownTracks.Add(bestRoughMatch.Song);
							else
								res.unknownTracks.Add(currentSong.songref);
						}
					}


					var nextSimlist = lookupParallel(currentSong.songref, res.similarList.Count);
					if (nextSimlist == null)
						continue;

					double usageMean = 0.0;
					foreach (var srcSong in currentSong.basedOn) {
						usageMean += (100.0 + usages[srcSong]);
						usages[srcSong] += nextSimlist.similartracks.Length;
					}
					usageMean /= currentSong.basedOn.Count;

					int simRank = 0;
					foreach (var similarTrack in nextSimlist.similartracks) {
						SongWithCost similarSong = songCostCache.Lookup(similarTrack.similarsong);
						double directCost = currentSong.cost + 0.01 + simRank / 20.0 + usageMean/200.0;
						simRank++;
						if (similarSong.cost <= currentSong.cost) //well, either we've already been processed, or we're already at the top spot in the heap: ignore.
							continue;
						else if (similarSong.index == -1) { //not in the heap.
							similarSong.cost = directCost;
							foreach (var baseSong in currentSong.basedOn)
								similarSong.basedOn.Add(baseSong);
							lock (sync)
								songCosts.Add(similarSong);
						} else {
							lock (sync) {
								songCosts.Delete(similarSong.index);
								similarSong.index = -1;

								//new cost should be somewhere between next.cost, and min(old-cost, direct-cost)
								double oldOffset = similarSong.cost - currentSong.cost;
								double newOffset = directCost - currentSong.cost;
								double combinedOffset = 1.0 / Math.Sqrt(1.0 / oldOffset * oldOffset + 1.0 / newOffset * newOffset);
								similarSong.cost = currentSong.cost + combinedOffset;
								//similarSong.cost = Math.Min(similarSong.cost, directCost);
								foreach (var baseSong in currentSong.basedOn)
									similarSong.basedOn.Add(baseSong);
								songCosts.Add(similarSong);
							}
						}
					}
					int newPercent = Math.Max((res.similarList.Count * 100) / MaxSuggestionLookupCount, (res.knownTracks.Count * 100) / SuggestionCountTarget);
					if (newPercent > lastPercent) {
						lastPercent = newPercent;
						string msg = "[" + songCosts.Count + ";" + newPercent + "%]";
						Console.Write(msg.PadRight(16, ' '));
					}
				}
			} finally {
				lock (sync) done = true;
			}
			Console.WriteLine("{0} similar tracks generated, of which {1} found locally.", res.similarList.Count, res.knownTracks.Count);
			return res;
		}

	}
}
