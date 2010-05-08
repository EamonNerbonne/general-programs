using System.Collections.Generic;

namespace LastFMspider {
	public static partial class FindSimilarPlaylist {
		public class SongWithCostCache {
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
	}
}