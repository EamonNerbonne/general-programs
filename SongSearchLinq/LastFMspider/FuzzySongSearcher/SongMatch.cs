using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;

namespace LastFMspider {
	public struct SongMatch : IComparable<SongMatch> {
		public double Cost;
		public SongFileData Song;
		public string Explain;
		public bool GoodEnough { get { return Cost < 1.5; } }

		public int CompareTo(SongMatch other) { return Cost.CompareTo(other.Cost); }

		//typically around 0.35, for really bad files rarely in excess of 1.0, for perfect files 0.0.
		public static double AbsoluteSongCost(SongFileData local) {
			return
				Math.Abs(Math.Log(local.bitrate) - 5.32) /* about 0 ... 0.5 */
				+ Math.Abs(Math.Log((local.Length + 1) / 216.0)); /* about 0 ... 0.5 */
		}

		//only sort by song quality, thus.
		public static SongMatch[] PerfectMatches(IEnumerable<SongFileData> matches) {
			var matchesWithCost = matches.Select(song => new SongMatch { Cost = AbsoluteSongCost(song), Song = song, }).ToArray();
			Array.Sort(matchesWithCost);
			return matchesWithCost;
		}

		public static SongMatch NoMatch { get { return new SongMatch { Cost = double.PositiveInfinity, Song = null, Explain = "No Match" }; } }
	}
}
