using System;
using System.Collections.Generic;
using LastFMspider.LastFMSQLiteBackend;
namespace LastFMspider {
	public static partial class FindSimilarPlaylist {
		public class SongWithCost : IComparable<SongWithCost> {
			public TrackId trackid;
			public double cost = double.PositiveInfinity;
			public int index = -1;
			public int graphDist = -1;
			public readonly HashSet<SongWithCost> basedOn = new HashSet<SongWithCost>();
			public readonly HashSet<SongWithCost> dependants = new HashSet<SongWithCost>();


			public int CompareTo(SongWithCost other) { return cost.CompareTo(other.cost); }
		}
	}
}