using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;

namespace LastFMspider {
	public struct SongMatch : IComparable<SongMatch> {
		readonly double trigramCost, artistCost, titleCost, artistCanonicalizedCost, titleCanonicalizedCost, absoluteQualityCost;
		public readonly double Cost;
		public readonly SongFileData Song;
		public SongMatch(SongFileData Song, double trigramCost = 0.0, double artistCost = 0.0, double titleCost = 0.0, double artistCanonicalizedCost = 0.0, double titleCanonicalizedCost = 0.0, double absoluteQualityCost = 0.0) {
			this.trigramCost = trigramCost;
			this.artistCost = artistCost;
			this.titleCost = titleCost;
			this.artistCanonicalizedCost = artistCanonicalizedCost;
			this.titleCanonicalizedCost = titleCanonicalizedCost;
			this.absoluteQualityCost = absoluteQualityCost;
			this.Song = Song;
			Cost = trigramCost + artistCost + titleCost + artistCanonicalizedCost + titleCanonicalizedCost + absoluteQualityCost;
		}

		public string Explain {
			get {
				return
					double.IsPositiveInfinity(absoluteQualityCost) ? "No Match" :
					trigramCost + " + "
					 + artistCost + " + "
					 + titleCost + " + "
					 + artistCanonicalizedCost + " + "
					 + titleCanonicalizedCost
					 + (absoluteQualityCost == 0.0 ? "" : " + " + absoluteQualityCost);
			}
		}

		public bool GoodEnough { get { return Cost < 1.45; } }

		public int CompareTo(SongMatch other) { return Cost.CompareTo(other.Cost); }

		//typically around 0.35, for really bad files rarely in excess of 1.0, for perfect files 0.0.
		public static double AbsoluteSongCost(SongFileData local) {
			return
				Math.Abs(Math.Log(local.bitrate) - 5.32) /* about 0 ... 0.5 */
				+ Math.Abs(Math.Log((local.Length + 1) / 216.0)); /* about 0 ... 0.5 */
		}

		//only sort by song quality, thus.
		public static SongMatch[] PerfectMatches(IEnumerable<SongFileData> matches) {
			var matchesWithCost = matches.Select(song => new SongMatch(song, absoluteQualityCost: AbsoluteSongCost(song))).ToArray();
			Array.Sort(matchesWithCost);
			return matchesWithCost;
		}

		public static SongMatch NoMatch { get { return new SongMatch(null, absoluteQualityCost: double.PositiveInfinity); } }
	}
}
