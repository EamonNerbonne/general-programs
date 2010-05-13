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
	[Serializable]
	public class SongSimilarityList {
		public SimilarTracksListId id;
		public SongRef songref;
		public SimilarTrack[] similartracks;
		public DateTime LookupTimestamp;
		public int? StatusCode;

		public static SongSimilarityList CreateErrorList(SongRef songref, int errorCode) { return new SongSimilarityList { LookupTimestamp = DateTime.UtcNow, StatusCode = errorCode, similartracks = new SimilarTrack[0], songref = songref }; }
	}

	public class TrackSimilarityList {

		public readonly SongRef SongRef;
		public readonly TrackId TrackId;
		public IEnumerable<SimilarTrackId> SimilarTracks { 
			get { return DbUtil.DecodeRatingBlob(similarTracks).Select(tup => new SimilarTrackId(tup.Item1, tup.Item2)); }
			set { similarTracks = DbUtil.EncodeRatingBlob(value.Select(rating => Tuple.Create(rating.TrackId.Id, rating.Similarity))); } 
		}
		public readonly DateTime LookupTimestamp;
		public readonly int? StatusCode;
		internal byte[] similarTracks = new byte[]{};

		public struct SimilarTrackId {
			public readonly TrackId TrackId;
			public readonly float Similarity;
			internal SimilarTrackId(uint id, float sim) { this.TrackId = new TrackId(id); this.Similarity = sim; }
		}
	}
}
