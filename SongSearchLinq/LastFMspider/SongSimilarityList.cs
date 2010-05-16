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


	public interface ICachedInfo<TSelf, TSelfId>
		where TSelfId : IId
		where TSelf : ICachedInfo<TSelf, TSelfId> {
		TSelfId ListID { get; }
		DateTime? LookupTimestamp { get; }
		int? StatusCode { get; }
	}


	public struct TrackSimilarityListInfo : ICachedInfo<TrackSimilarityListInfo,SimilarTracksListId> {
		public readonly SongRef SongRef;
		public readonly TrackId TrackId;

		readonly SimilarTracksListId _ListID;
		public SimilarTracksListId ListID { get { return _ListID; } }

		readonly DateTime? _LookupTimestamp;
		public DateTime? LookupTimestamp { get { return _LookupTimestamp; } }

		readonly SimilarityList<TrackId, TrackId.Factory> _SimilarTracks;
		public IEnumerable<SimilarityTo<TrackId>> SimilarTracks { get { return _SimilarTracks.Similarities; } }

		readonly int? _StatusCode;
		public int? StatusCode { get { return _StatusCode; } }

		internal TrackSimilarityListInfo(SimilarTracksListId listID, TrackId trackId, SongRef songref, DateTime? lookupTimestamp, int? statusCode,
			SimilarityList<TrackId, TrackId.Factory> similarTracks) {
			this.SongRef = songref; this.TrackId = trackId; this._ListID = listID; this._LookupTimestamp = lookupTimestamp;
			this._StatusCode = statusCode; this._SimilarTracks = similarTracks;
		}
		public static TrackSimilarityListInfo CreateUnknown(SongRef song, TrackId trackId) {
			return new TrackSimilarityListInfo(default(SimilarTracksListId), trackId, song, null, null, new SimilarityList<TrackId, TrackId.Factory>(new byte[] { }));
		}
	}
}
