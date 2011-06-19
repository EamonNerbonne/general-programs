using LastFMspider.LastFMSQLiteBackend;
using SongDataLib;

namespace LastFMspider {
	public struct SimilarTrack {
		public double similarity;
		public SongRef similarsong;
		public TrackId id;
	}
}
