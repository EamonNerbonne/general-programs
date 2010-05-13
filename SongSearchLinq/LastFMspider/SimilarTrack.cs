using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using LastFMspider.LastFMSQLiteBackend;

namespace LastFMspider {
	public struct SimilarTrack {
		public double similarity;
		public SongRef similarsong;
		public TrackId id;
	}
}
