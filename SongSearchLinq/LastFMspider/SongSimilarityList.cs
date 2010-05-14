using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using EmnExtensions.Text;
using System.Runtime.Serialization;
using LastFMspider.LastFMSQLiteBackend;

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

	public struct TrackSimilarityListInfo {
		public readonly SongRef SongRef;
		public readonly TrackId TrackId;
		public readonly SimilarTracksListId ListID;
		public readonly DateTime? LookupTimestamp;
		readonly SimilarityList<TrackId, TrackId.Factory> _SimilarTracks;
		public IEnumerable<SimilarityTo<TrackId>> SimilarTracks { get { return _SimilarTracks.Similarities; } }
		public readonly int? StatusCode;
		public TrackSimilarityListInfo(SimilarTracksListId listID, TrackId trackId, SongRef songref, DateTime? lookupTimestamp, int? statusCode,
			IEnumerable<SimilarityTo<TrackId>> sims) {
			this.SongRef = songref; this.TrackId = trackId; this.ListID = listID; this.LookupTimestamp = lookupTimestamp;
			this.StatusCode = statusCode; this._SimilarTracks = new SimilarityList<TrackId, TrackId.Factory>(sims);
		}
		public TrackSimilarityListInfo(SimilarTracksListId listID, TrackId trackId, SongRef songref, DateTime? lookupTimestamp, int? statusCode,
			byte[] sims) {
			this.SongRef = songref; this.TrackId = trackId; this.ListID = listID; this.LookupTimestamp = lookupTimestamp;
			this.StatusCode = statusCode; this._SimilarTracks = new SimilarityList<TrackId, TrackId.Factory>(sims ?? new byte[] { });
		}
		public static TrackSimilarityListInfo CreateUnknown(SongRef song,TrackId trackId) {
			return new TrackSimilarityListInfo(default(SimilarTracksListId), trackId, song, null, null, default(byte[]));
		}
	}
}
