using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.Text;
using EmnExtensions.DebugTools;
using SongDataLib;
using EmnExtensions.Algorithms;
using LastFMspider.FuzzySongSearcherInternal;

namespace LastFMspider {

	public class FuzzySongSearcher {
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
						Trigrammer.Trigrams(song.artist)
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

		public SongMatch FindBestMatch(SongRef search) {
			var matches = FindMatchingSongs(search);
			if (matches.Length > 0)
				return matches[0];
			else
				return SongMatch.NoMatch;
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
								+ (songs[songIndex].artist ?? "").ToLowerInvariant().LevenshteinDistanceScaled(search.Artist.ToLowerInvariant())
								+ (songs[songIndex].title ?? "").ToLowerInvariant().LevenshteinDistanceScaled(search.Title.ToLowerInvariant())
								+ (songs[songIndex].artist ?? "").CanonicalizeBasic().LevenshteinDistanceScaled(search.Artist.CanonicalizeBasic())
								+ (songs[songIndex].title ?? "").CanonicalizeBasic().LevenshteinDistanceScaled(search.Title.CanonicalizeBasic())
								+ 0.1 * SongMatch.AbsoluteSongCost(songs[songIndex]),
							Explain = "" + (1.0 - matchcounts[songIndex] / (double)Math.Max(searchTrigrams.Length, trigramCountBySong[songIndex])) + " + "
										+ (songs[songIndex].artist ?? "").ToLowerInvariant().LevenshteinDistanceScaled(search.Artist.ToLowerInvariant()) + " + "
										+ (songs[songIndex].title ?? "").ToLowerInvariant().LevenshteinDistanceScaled(search.Title.ToLowerInvariant()) + " + "
										+ (songs[songIndex].artist ?? "").CanonicalizeBasic().LevenshteinDistanceScaled(search.Artist.CanonicalizeBasic()) + " + "
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
}
