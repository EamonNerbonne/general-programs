using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LastFMspider.LastFMSQLiteBackend;

namespace LastFMspider {
	public struct SimilarArtist : IComparable<SimilarArtist> {
		public string Artist;//TODO: why no ID?
		public double Rating;

		public int CompareTo(SimilarArtist other) { return other.Rating.CompareTo(Rating); }
	}
	public class ArtistSimilarityList {
		public DateTime LookupTimestamp;//TODO why no ID?
		public string Artist;
		public SimilarArtist[] Similar;
		public int? StatusCode;

		public static ArtistSimilarityList CreateErrorList(string artist, int errCode) {
			return new ArtistSimilarityList {
				Artist = artist,
				LookupTimestamp = DateTime.UtcNow,
				Similar = new SimilarArtist[] { },
				StatusCode = errCode,
			};
		}
	}

	public struct ArtistSimilarityListInfo {
		public readonly ArtistInfo ArtistInfo;
		public readonly SimilarArtistsListId ListID;
		public readonly DateTime? LookupTimestamp;
		readonly SimilarityList<ArtistId, ArtistId.Factory> _SimilarArtists;
		public IEnumerable<SimilarityTo<ArtistId>> SimilarArtists { get { return _SimilarArtists.Similarities; } }
		public readonly int? StatusCode;
		internal ArtistSimilarityListInfo(SimilarArtistsListId listID, ArtistInfo artistInfo, DateTime? lookupTimestamp,
			int? statusCode, SimilarityList<ArtistId, ArtistId.Factory> similarArtists) {
			this.ArtistInfo = artistInfo;  this.ListID = listID; this.LookupTimestamp = lookupTimestamp;
			this.StatusCode = statusCode; this._SimilarArtists = similarArtists;
		}
		public static ArtistSimilarityListInfo CreateUnknown(ArtistInfo artist) {
			return new ArtistSimilarityListInfo(default(SimilarArtistsListId), artist, null, null, new SimilarityList<ArtistId, ArtistId.Factory>(new byte[]{}) );
		}
	}
}
