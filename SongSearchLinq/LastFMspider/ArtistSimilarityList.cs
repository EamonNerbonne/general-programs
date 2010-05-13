using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LastFMspider {
	public struct SimilarArtist : IComparable<SimilarArtist> {
		public string Artist;
		public double Rating;

		public int CompareTo(SimilarArtist other) { return other.Rating.CompareTo(Rating); }
	}
	public class ArtistSimilarityList {
		public DateTime LookupTimestamp;
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

}
