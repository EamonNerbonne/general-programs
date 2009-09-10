using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using EmnExtensions;
using EmnExtensions.Algorithms;
using EmnExtensions.Collections;
using EmnExtensions.DebugTools;
using EmnExtensions.Text;
using LastFMspider;
using MoreLinq;
using SongDataLib;

namespace LastFmPlaylistSuggestions
{
	public static class CharMap
	{
		static CharMap() {
			using (new DTimer("constructing map")) {
				uint[] retval = new uint[1 + (int)char.MaxValue]; //maps from char code to dense code.
				List<char> reverseMap = new List<char>();
				for (int i = 0; i < retval.Length; i++)
					try {
						foreach (char c in Canonicalize.Basic(new string(new[] { (char)i })))
							retval[(int)c]++;
					} catch (ArgumentException) { }// canonicalize may fail on invalid strings - i.e. chars with just
				//retval now contains the occurence count of a particular output charcode.
				reverseMap.Add((char)0xfffd);//0 == nonsense == mapped to replacement character;
				for (int i = 0; i < retval.Length; i++)
					if (i != 0xfffd && retval[i] > 0) { //0xfffd is already mapped, and if retval[i]==0, then it doesn't need mapping.
						retval[i] = (uint)reverseMap.Count;
						reverseMap.Add((char)i);
					}

				rawCharmap = retval;
				reverseCharmap = reverseMap.ToArray();
			}
		}

		static readonly uint[] rawCharmap;
		static readonly char[] reverseCharmap;
		public static uint MapChar(char c) { return rawCharmap[(int)c]; }
		public static char UnmapChar(uint i) { return reverseCharmap[i]; }
		/// <summary>
		/// if i is out of range, returns the unicode character 0xfffd (replacement char) instead of throwing an exception.
		/// </summary>
		public static char TryUnmapChar(uint i) { return i < reverseCharmap.Length ? reverseCharmap[i] : (char)0xfffd; }
		public static uint MapSize { get { return (uint)reverseCharmap.Length; } }

	}

	public static class Trigrammer
	{
		public static IEnumerable<uint> Trigrams(string input) {
			if (input == null)
				yield break;
			string canonicalized = Canonicalize.Basic(input);
			if (canonicalized.Length == 0)
				yield break;

			uint[] codes = canonicalized.PadLeft(3, (char)0xfffd).Select(c => CharMap.MapChar(c)).ToArray();
			for (int i = 0; i < codes.Length - 2; i++)
				yield return TrigramCode(codes[i], codes[i + 1], codes[i + 2]);
		}
		public static uint TrigramCode(uint a, uint b, uint c) { return a + b * CharMap.MapSize + c * CharMap.MapSize * CharMap.MapSize; }
		public static uint TrigramCount { get { return CharMap.MapSize * CharMap.MapSize * CharMap.MapSize; } }
	}

	public class FuzzySongSearcher
	{
		int[][] songsByTrigram;
		int[] trigramCountBySong;
		List<SongData> songs;
		public FuzzySongSearcher(LastFmTools tools) {
			using (new DTimer("Constructing FuzzySongSearcher")) {
				songs = tools.DB.Songs;
				uint[][] trigramsBySong = new uint[songs.Count][];
				trigramCountBySong = new int[songs.Count];
				int[] trigramOccurenceCount = new int[Trigrammer.TrigramCount];

				for (int i = 0; i < trigramsBySong.Length; i++) {
					SongData song = songs[i];
					trigramsBySong[i] =
						Trigrammer.Trigrams(song.performer)
						.Concat(Trigrammer.Trigrams(song.title))
						//.Concat(Trigrammer.Trigrams(Path.GetFileNameWithoutExtension(song.SongPath)))
						.Distinct()
						.ToArray();
					trigramCountBySong[i] = trigramsBySong[i].Length;
					foreach (uint trigram in trigramsBySong[i])
						trigramOccurenceCount[trigram]++;
				}

				songsByTrigram = new int[Trigrammer.TrigramCount][];
				for (int ti = 0; ti < Trigrammer.TrigramCount; ti++)
					songsByTrigram[ti] = new int[trigramOccurenceCount[ti]];//constructed arrays to hold lists of all songs for a given trigram.

				//trigramOccurenceCount: how many trigrams yet to process!

				for (int i = 0; i < trigramsBySong.Length; i++) { //for each song...
					foreach (uint trigram in trigramsBySong[i]) { //for each trigram of each song...
						//If there were N trigrams to be processed still, then N-1 is a valid, unoccupied index in the trigram list for that song.
						trigramOccurenceCount[trigram]--;
						songsByTrigram[trigram][trigramOccurenceCount[trigram]] = i;
					}
				}

				for (int ti = 0; ti < Trigrammer.TrigramCount; ti++)
					if (trigramOccurenceCount[ti] != 0)
						throw new ApplicationException("BUG: Invalid programming assumption; review code.");//constructed arrays to hold lists of all songs for a given trigram.
			}
		}


		public SongData FindBestMatch(SongRef search) {
			var matches = FindMatchingSongs(search);
			if (matches.Length > 0 && matches[0].Cost < 1.5)
				return matches[0].Song;
			else
				return null;
		}

		const int MaxMatchCount = 50;
		[ThreadStatic]
		int[] songmatchcount = null;
		public SongMatch[] FindMatchingSongs(SongRef search) {
			int[] matchcounts = songmatchcount;
			if (matchcounts == null)
				songmatchcount = matchcounts = new int[songs.Count];//cache to save mem-allocation overhead.
			uint[] searchTrigrams = Trigrammer.Trigrams(search.Artist).Concat(Trigrammer.Trigrams(search.Title)).Distinct().ToArray();
			try {

				foreach (uint trigram in searchTrigrams)
					foreach (int songIndex in songsByTrigram[trigram])
						matchcounts[songIndex]++;

				List<int> matchingSongs = new List<int>(50);
				int minimumMatchCount = (searchTrigrams.Length * 6 + 9) / 10;
				for (int i = 0; i < matchcounts.Length; i++) {
					if (matchcounts[i] >= minimumMatchCount)
						matchingSongs.Add(i);
					else
						matchcounts[i] = 0;
				}
				if (matchingSongs.Count > MaxMatchCount) { //too many, raise threshhold...
					matchingSongs.Sort((songA, songB) =>
						// if songA is better, return negative so better things come first.
						// songA is better than songB if songA has more matches.
						// so return matchcount of songB - matchcount of song A: when A has more matches, this is negative...
						matchcounts[songB] - matchcounts[songA]);
				}

				var q = from songIndex in matchingSongs.Take(MaxMatchCount)
						select new SongMatch {
							Song = songs[songIndex],
							Cost =
								 (1.0 - matchcounts[songIndex] / (double)Math.Max(searchTrigrams.Length, trigramCountBySong[songIndex]))
								+ (songs[songIndex].performer ?? "").ToLowerInvariant().LevenshteinDistanceScaled(search.Artist.ToLowerInvariant())
								+ (songs[songIndex].title ?? "").ToLowerInvariant().LevenshteinDistanceScaled(search.Title.ToLowerInvariant())
								+ (songs[songIndex].performer ?? "").CanonicalizeBasic().LevenshteinDistanceScaled(search.Artist.CanonicalizeBasic())
								+ (songs[songIndex].title ?? "").CanonicalizeBasic().LevenshteinDistanceScaled(search.Title.CanonicalizeBasic())
								+ 0.1 * SongMatch.AbsoluteSongCost(songs[songIndex]),
							Explain = "" + (1.0 - matchcounts[songIndex] / (double)Math.Max(searchTrigrams.Length, trigramCountBySong[songIndex])) + " + "
										+ (songs[songIndex].performer ?? "").ToLowerInvariant().LevenshteinDistanceScaled(search.Artist.ToLowerInvariant()) + " + "
										+ (songs[songIndex].title ?? "").ToLowerInvariant().LevenshteinDistanceScaled(search.Title.ToLowerInvariant()) + " + "
										+ (songs[songIndex].performer ?? "").CanonicalizeBasic().LevenshteinDistanceScaled(search.Artist.CanonicalizeBasic()) + " + "
										+ (songs[songIndex].title ?? "").CanonicalizeBasic().LevenshteinDistanceScaled(search.Title.CanonicalizeBasic()) + " + "
										+ 0.1 * SongMatch.AbsoluteSongCost(songs[songIndex]),
						};

				SongMatch[] matches = q.ToArray();
				Array.Sort(matches);

				for (int i = 0; i < matchingSongs.Count; i++)
					matchcounts[matchingSongs[i]] = 0;
				return matches;
			} catch {//hmm songmatchcount must be zeroed; we'll just throw it away.
				songmatchcount = null;
				throw;
			}
		}
	}

	public struct SongMatch : IComparable<SongMatch>
	{
		public double Cost;
		public SongData Song;
		public string Explain;

		public int CompareTo(SongMatch other) { return Cost.CompareTo(other.Cost); }

		//typically around 0.35, for really bad files rarely in excess of 1.0, for perfect files 0.0.
		public static double AbsoluteSongCost(SongData local) {
			return
				Math.Abs(Math.Log(local.bitrate) - 5.32) /* about 0 ... 0.5 */
				+ Math.Abs(Math.Log((local.Length + 1) / 216.0)); /* about 0 ... 0.5 */
		}

		//only sort by song quality, thus.
		public static SongMatch[] PerfectMatches(IEnumerable<SongData> matches) {
			var matchesWithCost = matches.Select(song => new SongMatch { Cost = AbsoluteSongCost(song), Song = song, }).ToArray();
			Array.Sort(matchesWithCost);
			return matchesWithCost;
		}
	}

	class Program
	{
		const int MaxSuggestionLookupCount = 1000;
		const int SuggestionCountTarget = 100;
		static void Main(string[] args) {
			//args.PrintAllDebug();
			//Console.ReadLine();
			//Console.WriteLine(CharMap.MapSize);
			RunNew(new LastFmTools(new SongDatabaseConfigFile(false)), args);
			//Console.ReadKey();
		}
		static void RunNew(LastFmTools tools, string[] args) {
			var dir = tools.DB.DatabaseDirectory.CreateSubdirectory("inputlists");
			var m3us = args.Length == 0 ? dir.GetFiles("*.m3u?") : args.Select(s => new FileInfo(s)).Where(f => f.Exists);
			DirectoryInfo m3uDir = args.Length == 0 ? tools.DB.DatabaseDirectory.CreateSubdirectory("similarlists") : new DirectoryInfo(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

			FuzzySongSearcher searchEngine = new FuzzySongSearcher(tools);

			foreach (var m3ufile in m3us) {
				try {
					using (var dTimer = new DTimer("Processing " + m3ufile.Name))
						ProcessM3U(tools, searchEngine, m3ufile, m3uDir);
				} catch (Exception e) {
					Console.WriteLine("Unexpected error on processing " + m3ufile);
					Console.WriteLine(e.ToString());
				}
			}
		}

		static void FindPlaylistSongLocally(LastFmTools tools, ISongData playlistEntry, Action<SongData> ifFound, Action<SongRef> ifNotFound, Action<ISongData> cannotParse) {
			SongData bestMatch = null;
			int artistTitleSplitIndex = playlistEntry.HumanLabel.IndexOf(" - ");
			if (tools.Lookup.dataByPath.ContainsKey(playlistEntry.SongPath))
				ifFound(tools.Lookup.dataByPath[playlistEntry.SongPath]);
			else if (playlistEntry.IsLocal && File.Exists(playlistEntry.SongPath)) {
				ifFound((SongData)SongDataFactory.ConstructFromFile(new FileInfo(playlistEntry.SongPath)));
			} else if (playlistEntry is PartialSongData) {
				int bestMatchVal = Int32.MaxValue;
				while (artistTitleSplitIndex != -1) {
					SongRef songref = SongRef.Create(playlistEntry.HumanLabel.Substring(0, artistTitleSplitIndex), playlistEntry.HumanLabel.Substring(artistTitleSplitIndex + 3));
					if (tools.Lookup.dataByRef.ContainsKey(songref)) {
						foreach (var songCandidate in tools.Lookup.dataByRef[songref]) {
							int candidateMatchVal = 100 * Math.Abs(playlistEntry.Length - songCandidate.Length) + Math.Min(199, Math.Abs(songCandidate.bitrate - 224));
							if (candidateMatchVal < bestMatchVal) {
								bestMatchVal = candidateMatchVal;
								bestMatch = songCandidate;
							}
						}
					}
					artistTitleSplitIndex = playlistEntry.HumanLabel.IndexOf(" - ", artistTitleSplitIndex + 3);
				}
				if (bestMatch != null) ifFound(bestMatch);
				else {
					artistTitleSplitIndex = playlistEntry.HumanLabel.IndexOf(" - ");
					if (artistTitleSplitIndex >= 0) ifNotFound(SongRef.Create(playlistEntry.HumanLabel.Substring(0, artistTitleSplitIndex), playlistEntry.HumanLabel.Substring(artistTitleSplitIndex + 3)));
					else cannotParse(playlistEntry);
				}
			} else {
				cannotParse(playlistEntry);
			}
		}

		public class SongWithCost : IComparable<SongWithCost>
		{
			public SongRef songref;
			public double cost = double.PositiveInfinity;
			public int index = -1;
			public HashSet<SongRef> basedOn = new HashSet<SongRef>();

			public int CompareTo(SongWithCost other) { return cost.CompareTo(other.cost); }
		}

		public class SongWithCostCache
		{
			Dictionary<SongRef, SongWithCost> songCostLookupDict = new Dictionary<SongRef, SongWithCost>();
			public SongWithCost Lookup(SongRef song) {
				SongWithCost retval;
				if (songCostLookupDict.TryGetValue(song, out retval))
					return retval;
				retval = new SongWithCost { songref = song };
				songCostLookupDict.Add(song, retval);
				return retval;
			}
		}

		static void ProcessM3U(LastFmTools tools, FuzzySongSearcher searchEngine, FileInfo m3ufile, DirectoryInfo m3uDir) {
			Console.WriteLine("Trying " + m3ufile.FullName);
			var playlist = LoadExtM3U(m3ufile);
			var known = new List<SongData>();
			var unknown = new List<SongRef>();
			foreach (var song in playlist)
				FindPlaylistSongLocally(tools, song,
					(songData) => { known.Add(songData); },
					(songRef) => { unknown.Add(songRef); },
					(oopsSong) => { Console.WriteLine("Can't deal with: " + oopsSong.HumanLabel + "\nat:" + oopsSong.SongPath); }
				);
			//OK, so we now have the playlist in the var "playlist" with knowns in "known" except for the unknowns, which are in "unknown" as far as possible.

			var playlistSongRefs = new HashSet<SongRef>(known.Select(sd => SongRef.Create(sd)).Where(sr => sr != null).Cast<SongRef>().Concat(unknown));


			SongWithCostCache songCostCache = new SongWithCostCache();
			Heap<SongWithCost> songCosts = new Heap<SongWithCost>((sc, index) => { sc.index = index; });
			List<SongWithCost> similarList = new List<SongWithCost>();

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
				new Thread(bgLookup) { IsBackground = true, Priority = ThreadPriority.BelowNormal }.Start();
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


			playlistSongRefs
				.Select(songref => songCostCache.Lookup(songref))
				.ForEach(songcost => { songcost.cost = 0.0; songcost.basedOn.Add(songcost.songref); songCosts.Add(songcost); });

			List<SongData> knownTracks = new List<SongData>();
			List<SongRef> unknownTracks = new List<SongRef>();

			int lastPercent = 0;
			try {
				while (similarList.Count < MaxSuggestionLookupCount && knownTracks.Count < SuggestionCountTarget) {
					SongWithCost currentSong;
					if (!songCosts.RemoveTop(out currentSong))
						break;
					if (!playlistSongRefs.Contains(currentSong.songref)) {
						similarList.Add(currentSong);
						if (tools.Lookup.dataByRef.ContainsKey(currentSong.songref))
							knownTracks.Add((
								from songcandidate in tools.Lookup.dataByRef[currentSong.songref]
								orderby SongMatch.AbsoluteSongCost(songcandidate)
								select songcandidate
							).First());
						else {
							SongData bestRoughMatch = searchEngine.FindBestMatch(currentSong.songref);
							if (bestRoughMatch != null)
								knownTracks.Add(bestRoughMatch);
							else
								unknownTracks.Add(currentSong.songref);
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
							songCosts.Add(similarSong);
						} else {
							songCosts.Delete(similarSong.index);
							similarSong.index = -1;
							//new cost should be somewhere between next.cost, and min(old-cost, direct-cost)
							double oldOffset = similarSong.cost - currentSong.cost;
							double newOffset = directCost - currentSong.cost;
							double combinedOffset = 1.0 / (1.0 / oldOffset + 1.0 / newOffset);
							similarSong.cost = currentSong.cost + combinedOffset;
							foreach (var baseSong in currentSong.basedOn)
								similarSong.basedOn.Add(baseSong);
							songCosts.Add(similarSong);
						}
					}
					int newPercent = Math.Max((similarList.Count * 100) / MaxSuggestionLookupCount, (knownTracks.Count * 100) / SuggestionCountTarget);
					if (newPercent > lastPercent) {
						lastPercent = newPercent;
						string msg = "[" + songCosts.Count + ";" + newPercent + "%]";
						Console.Write(msg.PadRight(16, ' '));
					}

				}
			} finally {
				lock (sync) done = true;
			}

			Console.WriteLine("{0} similar tracks generated, of which {1} found locally.", similarList.Count, knownTracks.Count);

			FileInfo outputplaylist = new FileInfo(Path.Combine(m3uDir.FullName, Path.GetFileNameWithoutExtension(m3ufile.Name) + "-similar.m3u"));
			using (var stream = outputplaylist.Open(FileMode.Create))
			using (var writer = new StreamWriter(stream, Encoding.GetEncoding(1252))) {
				writer.WriteLine("#EXTM3U");
				foreach (var track in knownTracks) {
					writer.WriteLine("#EXTINF:" + track.Length + "," + track.HumanLabel + "\n" + track.SongPath);
				}
			}
			FileInfo outputsimtracks = new FileInfo(Path.Combine(m3uDir.FullName, Path.GetFileNameWithoutExtension(m3ufile.Name) + "-similar.txt"));
			using (var stream = outputsimtracks.Open(FileMode.Create))
			using (var writer = new StreamWriter(stream, Encoding.UTF8)) {
				foreach (var track in similarList) {
					writer.WriteLine("{0} {2} {1}    [{3}]",
							track.cost,
							track.songref.ToString(),
							tools.Lookup.dataByRef.ContainsKey(track.songref) ? "" : "***",
							string.Join(";  ", track.basedOn.Select(sr => sr.ToString()).ToArray())
						);
				}
			}
			FileInfo outputmissingtracks = new FileInfo(Path.Combine(m3uDir.FullName, Path.GetFileNameWithoutExtension(m3ufile.Name) + "-missing.txt"));
			//using(new DTimer("finding missing tracks"))
			using (var stream = outputmissingtracks.Open(FileMode.Create))
			using (var writer = new StreamWriter(stream, Encoding.UTF8)) {
				//var q = from songdata in tools.DB.Songs.Where(songdata=>songdata.performer!=null&& songdata.title!=null)
				//        from trigram in 
				foreach (var missingTrack in
					from track in unknownTracks
					let bestMatch = searchEngine.FindMatchingSongs(track).Take(1).ToArray()
					let orderCost = bestMatch.Length < 1 ? double.MaxValue : bestMatch[0].Cost
					orderby orderCost
					select new { Search = track, Match = bestMatch.FirstOrDefault().Song, Cost = bestMatch.FirstOrDefault().Cost, Explain = bestMatch.FirstOrDefault().Explain }
					) {
					if (missingTrack.Match != null) {
						writer.WriteLine("{0}      {1}    ||{2}: {3} - {4}   --  {5} at {6}kbps ||{7}", missingTrack.Search.Artist, missingTrack.Search.Title, missingTrack.Cost, missingTrack.Match.performer ?? "?", missingTrack.Match.title ?? "?", TimeSpan.FromSeconds(missingTrack.Match.Length), missingTrack.Match.bitrate, missingTrack.Explain);
					} else {
						writer.WriteLine("{0}      {1}", missingTrack.Search.Artist, missingTrack.Search.Title);
					}
				}
			}
		}

		static ISongData[] LoadExtM3U(FileInfo m3ufile) {
			List<ISongData> m3usongs = new List<ISongData>();
			using (var m3uStream = m3ufile.OpenRead()) {
				SongDataFactory.LoadSongsFromM3U(
					m3uStream, (newsong, completion) => { m3usongs.Add(newsong); },
					m3ufile.Extension.EndsWith("8") ? Encoding.UTF8 : Encoding.GetEncoding(1252),
					true
					);
			}
			return m3usongs.ToArray();
		}
	}
}
