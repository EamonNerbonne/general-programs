using System;
using System.Collections.Generic;
using System.Linq;
using EmnExtensions.Text;
using EmnExtensions.DebugTools;
using SongDataLib;
using EmnExtensions.Algorithms;
using LastFMspider.FuzzySongSearcherInternal;

namespace LastFMspider {

	public class FuzzySongSearcher {
		readonly int[][] songsByTrigram;
		readonly int[] trigramCountBySong;
		public readonly SongFileData[] songs;
		public FuzzySongSearcher(IEnumerable<SongFileData> psongs) {
			using (new DTimer("Constructing FuzzySongSearcher")) {
				songs = psongs.ToArray();
				uint[][] trigramsBySong = new uint[songs.Length][];
				trigramCountBySong = new int[songs.Length];
				int[] trigramOccurenceCount = new int[Trigrammer.TrigramCount];

				for (int i = 0; i < trigramsBySong.Length; i++) {
					SongFileData song = songs[i];
					trigramsBySong[i] =
						Trigrammer.Trigrams(song.artist)
						.Concat(Trigrammer.Trigrams(song.title))
						.Distinct()
						.ToArray();
					trigramCountBySong[i] = trigramsBySong[i].Length;
					foreach (uint trigram in trigramsBySong[i])
						trigramOccurenceCount[trigram]++;
				}

				songsByTrigram = new int[Trigrammer.TrigramCount][];
				for (int ti = 0; ti < Trigrammer.TrigramCount; ti++)
					songsByTrigram[ti] = new int[trigramOccurenceCount[ti]];//constructed arrays to hold lists of all songs for a given trigram.


				for (int ti = 0; ti < Trigrammer.TrigramCount; ti++)
					trigramOccurenceCount[ti] = 0;

				//songsByTrigram[ti].Length: how many trigrams we need to process!
				//trigramOccurenceCount[ti]: index of next trigram to process

				for (int i = 0; i < trigramsBySong.Length; i++) //for each song...
					foreach (uint trigram in trigramsBySong[i])  //for each trigram of each song...
						songsByTrigram[trigram][trigramOccurenceCount[trigram]++] = i;

				//songsByTrigram is in ascending order for any trigram!

				for (int trigram = 0; trigram < Trigrammer.TrigramCount; trigram++)
					if (trigramOccurenceCount[trigram] != songsByTrigram[trigram].Length)
						throw new ApplicationException("BUG: Invalid programming assumption; review code.");//constructed arrays to hold lists of all songs for a given trigram.
			}
		}

		public SongFileData FindAcceptableMatch(SongRef search) {
			var matches = FindMatchingSongs(search, MaxMatchCount:1);
			if (matches.Length > 0 && matches[0].GoodEnough)
				return matches[0].Song;
			else
				return null;
		}
		public SongMatch FindAnyMatch(SongRef search) {
			var matches = FindMatchingSongs(search, MaxMatchCount: 1);
			if (matches.Length > 0 )
				return matches[0];
			else
				return SongMatch.NoMatch;
		}

		const int MaxMatchCountDefault = 50;
		[ThreadStatic]
		static int[] songmatchcount;
		public SongMatch[] FindMatchingSongs(SongRef search, bool suppressAbsoluteCost = false, int MaxMatchCount = 0) {
			MaxMatchCount = MaxMatchCount == 0 ? MaxMatchCountDefault : MaxMatchCount;
			int[] matchcounts = songmatchcount;
			if (matchcounts == null)
				songmatchcount = matchcounts = new int[songs.Length];//cache to save mem-allocation overhead.
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
						let absoluteQualityCost = (suppressAbsoluteCost ? 0.0 : 0.1 * SongMatch.AbsoluteSongCost(songs[songIndex]))
						let titleCanonicalizedCost = (songs[songIndex].title ?? "").CanonicalizeBasic().LevenshteinDistanceScaled(search.Title.CanonicalizeBasic())
						let artistCanonicalizedCost = (songs[songIndex].artist ?? "").CanonicalizeBasic().LevenshteinDistanceScaled(search.Artist.CanonicalizeBasic())
						let titleCost = (songs[songIndex].title ?? "").ToLowerInvariant().LevenshteinDistanceScaled(search.Title.ToLowerInvariant())
						let artistCost = (songs[songIndex].artist ?? "").ToLowerInvariant().LevenshteinDistanceScaled(search.Artist.ToLowerInvariant())
						let trigramCost = (1.0 - matchcounts[songIndex] / (double)Math.Max(searchTrigrams.Length, trigramCountBySong[songIndex]))
						select new SongMatch(songs[songIndex], trigramCost, artistCost, titleCost, artistCanonicalizedCost, titleCanonicalizedCost, absoluteQualityCost);


				SongMatch[] matches = q.ToArray();
				Array.Sort(matches);

				foreach (int songIndex in matchingSongs)
					matchcounts[songIndex] = 0;
				return matches;
			} catch {//hmm songmatchcount must be zeroed; we'll just throw it away.
				songmatchcount = null;
				throw;
			}
		}
		public IEnumerable<SongFileData> FindPerfectMatchingSongs(SongRef search) {
			uint[] searchTrigrams = Trigrammer.Trigrams(search.Artist).Concat(Trigrammer.Trigrams(search.Title)).Distinct().ToArray();
			return
				SortedIntersectionAlgorithm.SortedIntersection(
					(from trigram in searchTrigrams
					 orderby songsByTrigram[trigram].Length
					 select songsByTrigram[trigram]).ToArray()).Where(songIndex => trigramCountBySong[songIndex] == searchTrigrams.Length).Select(songIndex => songs[songIndex]);
		}
	}
}
