using System;
using System.Collections.Generic;
using LastFMspider.LastFMSQLiteBackend;
using SongDataLib;
using TrackSimListStore = LastFMspider.SimilarityList<LastFMspider.LastFMSQLiteBackend.TrackId, LastFMspider.LastFMSQLiteBackend.TrackId.Factory>;

namespace LastFMspider {
	// status codes:
	// negative: non-problematic error (-1 == http404)
	// 0: no error, list is accurate
	// positive: list request error; list is empty but that might be an error.
	// 1: unknown exception occurred (DB locked?)
	// 2-22: WebException occured
	// 32: InvalidOperationException occurred.

	public class SongSimilarityList {
		public SongRef songref;
		public SimilarTrack[] similartracks;
		public int? StatusCode;

		public static SongSimilarityList CreateErrorList(SongRef songref, int errorCode) { return new SongSimilarityList { StatusCode = errorCode, similartracks = new SimilarTrack[0], songref = songref }; }
	}


	public struct TrackSimilarityListInfo {
		public readonly TrackId TrackId;

		readonly TrackSimListStore _SimilarTracks;
		public IEnumerable<SimilarityTo<TrackId>> SimilarTracks { get { return _SimilarTracks.Similarities; } }

		readonly int _StatusCode;
		public int StatusCode { get { return _StatusCode; } }

		internal TrackSimilarityListInfo(TrackId trackId, DateTime? lookupTimestamp, int statusCode,
			SimilarityList<TrackId, TrackId.Factory> similarTracks) {
			TrackId = trackId;
			_StatusCode = statusCode; _SimilarTracks = similarTracks;
		}
		public static TrackSimilarityListInfo CreateUnknown(TrackId trackId) {
			return new TrackSimilarityListInfo(trackId,  null, 1, default(TrackSimListStore));
		}
	}
}
