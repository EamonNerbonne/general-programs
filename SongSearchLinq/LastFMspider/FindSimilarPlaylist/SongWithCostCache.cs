using System.Collections.Generic;
using LastFMspider.LastFMSQLiteBackend;

namespace LastFMspider {
	public static partial class FindSimilarPlaylist {
		public class SongWithCostCache {
			readonly Dictionary<TrackId, SongWithCost> songCostLookupDict = new Dictionary<TrackId, SongWithCost>();
			public SongWithCost Lookup(TrackId trackid) {
				SongWithCost retval;
				if (songCostLookupDict.TryGetValue(trackid, out retval))
					return retval;
				retval = new SongWithCost { trackid = trackid };
				songCostLookupDict.Add(trackid, retval);
				return retval;
			}
		}
	}
}