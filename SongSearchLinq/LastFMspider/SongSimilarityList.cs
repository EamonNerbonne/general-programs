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
		public SimilarTracksListId id;
		public SongRef songref;
		public SimilarTrack[] similartracks;
		public DateTime LookupTimestamp;
		public int? StatusCode;

		public static SongSimilarityList CreateErrorList(SongRef songref, int errorCode) { return new SongSimilarityList { LookupTimestamp = DateTime.UtcNow, StatusCode = errorCode, similartracks = new SimilarTrack[0], songref = songref }; }
	}


	public struct TrackSimilarityListInfo : ICachedInfo<TrackSimilarityListInfo,SimilarTracksListId> {
		public readonly TrackId TrackId;

		readonly SimilarTracksListId _ListID;
		public SimilarTracksListId ListID { get { return _ListID; } }

		readonly DateTime? _LookupTimestamp;
		public DateTime? LookupTimestamp { get { return _LookupTimestamp; } }

		readonly TrackSimListStore _SimilarTracks;
		public IEnumerable<SimilarityTo<TrackId>> SimilarTracks { get { return _SimilarTracks.Similarities; } }

		readonly int? _StatusCode;
		public int? StatusCode { get { return _StatusCode; } }

		internal TrackSimilarityListInfo(SimilarTracksListId listID, TrackId trackId, DateTime? lookupTimestamp, int? statusCode,
			SimilarityList<TrackId, TrackId.Factory> similarTracks) {
			TrackId = trackId; _ListID = listID; _LookupTimestamp = lookupTimestamp;
			_StatusCode = statusCode; _SimilarTracks = similarTracks;
		}
		public static TrackSimilarityListInfo CreateUnknown(TrackId trackId) {
			return new TrackSimilarityListInfo(default(SimilarTracksListId), trackId,  null, null, default(TrackSimListStore));
		}
	}
}
