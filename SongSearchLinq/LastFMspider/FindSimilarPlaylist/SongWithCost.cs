using System;
using System.Collections.Generic;
namespace LastFMspider {
	public static partial class FindSimilarPlaylist {
		public class SongWithCost : IComparable<SongWithCost> {
			public SongRef songref;
			public double cost = double.PositiveInfinity;
			public int index = -1;
			public HashSet<SongRef> basedOn = new HashSet<SongRef>();

			public int CompareTo(SongWithCost other) { return cost.CompareTo(other.cost); }
		}
	}
}