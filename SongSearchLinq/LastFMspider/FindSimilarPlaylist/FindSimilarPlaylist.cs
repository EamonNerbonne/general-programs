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

		public static SimilarPlaylistResults ProcessPlaylist(LastFmTools tools, Func<SongRef, SongMatch> fuzzySearch, List<SongData> known, List<SongRef> unknown,
			int MaxSuggestionLookupCount = 20, int SuggestionCountTarget = 100
			) {
			//OK, so we now have the playlist in the var "playlist" with knowns in "known" except for the unknowns, which are in "unknown" as far as possible.

			var playlistSongRefs = new HashSet<SongRef>(known.Select(sd => SongRef.Create(sd)).Where(sr => sr != null).Cast<SongRef>().Concat(unknown));
			SimilarPlaylistResults res = new SimilarPlaylistResults();

			SongWithCostCache songCostCache = new SongWithCostCache();
			Heap<SongWithCost> songCosts = new Heap<SongWithCost>((sc, index) => { sc.index = index; });

			HashSet<SongRef> lookupsStarted = new HashSet<SongRef>();
			Dictionary<SongRef, SongSimilarityList> cachedLookup = new Dictionary<SongRef, SongSimilarityList>();
			Queue<SongRef> cacheOrder = new Queue<SongRef>();
			HashSet<SongRef> lookupsDeleted = new HashSet<SongRef>();

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
							.Where(songref => !lookupsStarted.Contains(songref))
							.FirstOrDefault();
						if (nextToLookup != null)
							lookupsStarted.Add(nextToLookup);
					}
					if (nextToLookup != null) {
						simList = tools.SimilarSongs.Lookup(nextToLookup);
						lock (sync) {
							cachedLookup[nextToLookup] = simList;
							cacheOrder.Enqueue(nextToLookup);
							while (cacheOrder.Count > 10000) {
								SongRef toRemove = cacheOrder.Dequeue();
								cachedLookup.Remove(cacheOrder.Dequeue());
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


			for (int i = 0; i < 8; i++) {
				new Thread(bgLookup) { Priority = ThreadPriority.BelowNormal }.Start();
			}

			Func<SongRef, SongSimilarityList> lookupParallel = songref => {
				SongSimilarityList retval;
				bool notInQueue;
				bool alreadyDeleted;
				while (true) {
					lock (sync) {
						if (cachedLookup.TryGetValue(songref, out retval))
							return retval; //easy case
						alreadyDeleted = lookupsDeleted.Contains(songref);
						notInQueue = !lookupsStarted.Contains(songref);
						if (notInQueue)
							lookupsStarted.Add(songref);
					}
					if (alreadyDeleted)
						return tools.SimilarSongs.Lookup(songref);
					if (notInQueue) {
						retval = tools.SimilarSongs.Lookup(songref);
						lock (sync) lookupsDeleted.Add(songref);
					}
					//OK, so song is in queue, not in cache but not deleted from cache: song must be in flight: we wait and then try again.
					Thread.Sleep(10);
				}
			};


			foreach (var songcost in playlistSongRefs.Select(songref => songCostCache.Lookup(songref))) {
				songcost.cost = 0.0;
				songcost.basedOn.Add(songcost.songref);
				songCosts.Add(songcost);
			}


			int lastPercent = 0;
			try {
				while (res.similarList.Count < MaxSuggestionLookupCount && res.knownTracks.Count < SuggestionCountTarget) {
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


					var nextSimlist = lookupParallel(currentSong.songref);
					if (nextSimlist == null)
						continue;

					int simRank = 0;
					foreach (var similarTrack in nextSimlist.similartracks) {
						SongWithCost similarSong = songCostCache.Lookup(similarTrack.similarsong);
						double directCost = currentSong.cost + 1.0 + simRank / 20.0;
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
							lock (sync)
								songCosts.Delete(similarSong.index);
							similarSong.index = -1;
							//new cost should be somewhere between next.cost, and min(old-cost, direct-cost)
							double oldOffset = similarSong.cost - currentSong.cost;
							double newOffset = directCost - currentSong.cost;
							double combinedOffset = 1.0 / (1.0 / oldOffset + 1.0 / newOffset);
							similarSong.cost = currentSong.cost + combinedOffset;
							foreach (var baseSong in currentSong.basedOn)
								similarSong.basedOn.Add(baseSong);
							lock (sync)
								songCosts.Add(similarSong);
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
