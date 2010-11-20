using System;
using System.Collections.Generic;
using System.Linq;
using SongDataLib;
using EmnExtensions.Collections;
using System.Threading;
using LastFMspider.LastFMSQLiteBackend;

namespace LastFMspider {
	public static partial class FindSimilarPlaylist {

		public class SimilarPlaylistResults {
			public readonly List<SongData> knownTracks = new List<SongData>();
			public readonly List<SongRef> unknownTracks = new List<SongRef>();
			public readonly List<SongWithCost> similarList = new List<SongWithCost>();
			public int LookupsDone;
		}
		static bool NeverAbort(int i) { return false; }

		public static SimilarPlaylistResults ProcessPlaylist(LastFmTools tools, Func<SongRef, SongMatch> fuzzySearch, IEnumerable<SongRef> seedSongs,
			int MaxSuggestionLookupCount = 100, int SuggestionCountTarget = 50, Func<int, bool> shouldAbort = null
			) {
			//OK, so we now have the playlist in the var "playlist" with knowns in "known" except for the unknowns, which are in "unknown" as far as possible.
			shouldAbort = shouldAbort ?? NeverAbort;

			var playlistSongs = seedSongs.Select(tools.SimilarSongs.backingDB.LookupTrackID.Execute).Reverse().ToArray();
			Dictionary<TrackId, HashSet<TrackId>> playlistSongRefs = playlistSongs.ToDictionary(sr => sr, sr => new HashSet<TrackId>());
			SimilarPlaylistResults res = new SimilarPlaylistResults();

			SongWithCostCache songCostCache = new SongWithCostCache();
			IHeap<SongWithCost> songCosts = Heap.Factory<SongWithCost>().Create((sc, index) => { sc.index = index; });

			HashSet<TrackId> lookupQueue = new HashSet<TrackId>();
			Dictionary<TrackId, TrackSimilarityListInfo> lookupCache = new Dictionary<TrackId, TrackSimilarityListInfo>();
			Queue<TrackId> lookupCacheOrder = new Queue<TrackId>();
			HashSet<TrackId> lookupsDeleted = new HashSet<TrackId>();

			var simSongDb = tools.SimilarSongs.backingDB;//ensure similarsongs loaded.
			object sync = new object();
			bool done = false;
			ThreadStart bgLookup = () => {
				bool tDone;
				// ReSharper disable AccessToModifiedClosure
				lock (sync) tDone = done;
				// ReSharper restore AccessToModifiedClosure
				while (!tDone) {
					TrackId nextToLookup;
					lock (sync) {
						nextToLookup =
							songCosts.ElementsInRoughOrder
							.Select(sc => sc.trackid)
							.Where(trackid => !lookupQueue.Contains(trackid))
							.FirstOrDefault();
						if (nextToLookup.HasValue)
							lookupQueue.Add(nextToLookup);
					}
					if (nextToLookup.HasValue) {
						Interlocked.Increment(ref res.LookupsDone);
						TrackSimilarityListInfo simList = simSongDb.LookupSimilarityListInfo.Execute(nextToLookup);
						lock (sync) {
							lookupCache[nextToLookup] = simList;
							lookupCacheOrder.Enqueue(nextToLookup);
							while (lookupCacheOrder.Count > 10000) {
								var toRemove = lookupCacheOrder.Dequeue();
								lookupCache.Remove(toRemove);
								lookupsDeleted.Add(toRemove);
							}
							//todo:notify
						}
					} else {
						Thread.Sleep(100);
					}

					// ReSharper disable AccessToModifiedClosure
					lock (sync) tDone = done;
					// ReSharper restore AccessToModifiedClosure
				}
			};



			Func<TrackId, int, TrackSimilarityListInfo> lookupParallel = (trackid, curcount) => {
				while (true) {
					bool notInQueue;
					bool alreadyDeleted;
					TrackSimilarityListInfo retval;
					lock (sync) {
						if (lookupCache.TryGetValue(trackid, out retval))
							return retval; //easy case
						alreadyDeleted = lookupsDeleted.Contains(trackid);
						notInQueue = !lookupQueue.Contains(trackid);
						if (notInQueue)
							lookupQueue.Add(trackid);
					}
					if (alreadyDeleted) {
						Interlocked.Increment(ref res.LookupsDone);
						return simSongDb.LookupSimilarityListInfo.Execute(trackid);
					}
					if (notInQueue) {
						Interlocked.Increment(ref res.LookupsDone);
						retval = simSongDb.LookupSimilarityListInfo.Execute(trackid);
						lock (sync) lookupsDeleted.Add(trackid);
						return retval;
					}
					//OK, so song is in queue, not in cache but not deleted from cache: song must be in flight: we wait and then try again.
					Thread.Sleep(1);
					if (shouldAbort(curcount)) return TrackSimilarityListInfo.CreateUnknown(trackid);
				}
			};


			foreach (var songcost in playlistSongs.Select(songCostCache.Lookup)) {
				songcost.cost = 0.0;
				songcost.graphDist = 0;
				songcost.basedOn.Add(songcost.trackid);
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
					currentSong.index = -1;
					if (!playlistSongRefs.ContainsKey(currentSong.trackid)) {
						SongRef currentSongRef = simSongDb.LookupTrack.Execute(currentSong.trackid);
						res.similarList.Add(currentSong);
						if (tools.Lookup.dataByRef.ContainsKey(currentSongRef))
							res.knownTracks.Add((
								from songcandidate in tools.Lookup.dataByRef[currentSongRef]
								orderby SongMatch.AbsoluteSongCost(songcandidate)
								select songcandidate
							).First());
						else {
							SongMatch bestRoughMatch = fuzzySearch(currentSongRef);
							if (bestRoughMatch.GoodEnough)
								res.knownTracks.Add(bestRoughMatch.Song);
							else
								res.unknownTracks.Add(currentSongRef);
						}
					}


					var nextSimlist = lookupParallel(currentSong.trackid, res.similarList.Count);


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
							lock (sync)
								songCosts.Add(similarSong);
						} else { // still in heap
							lock (sync) {
								songCosts.Delete(similarSong.index);
								similarSong.cost = directCost;
								similarSong.index = -1;
								songCosts.Add(similarSong);
							}
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
			} finally {
				lock (sync) done = true;
			}
			Console.WriteLine("{0} similar tracks generated, of which {1} found locally.", res.similarList.Count, res.knownTracks.Count);
			return res;
		}
	}
}
