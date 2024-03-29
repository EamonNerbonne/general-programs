﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LastFMspider.LastFMSQLiteBackend;
using ArtistSimListStore = LastFMspider.SimilarityList<LastFMspider.LastFMSQLiteBackend.ArtistId, LastFMspider.LastFMSQLiteBackend.ArtistId.Factory>;

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

		readonly SimilarArtistsListId _ListID;
		public SimilarArtistsListId ListID { get { return _ListID; } }

		readonly DateTime? _LookupTimestamp;
		public DateTime? LookupTimestamp { get { return _LookupTimestamp; } }

		readonly ArtistSimListStore _SimilarArtists;
		public IEnumerable<SimilarityTo<ArtistId>> SimilarArtists { get { return _SimilarArtists.Similarities; } }

		readonly int? _StatusCode;
		public int? StatusCode { get { return _StatusCode; } }

		internal ArtistSimilarityListInfo(SimilarArtistsListId listID, ArtistInfo artistInfo, DateTime? lookupTimestamp,
			int? statusCode, SimilarityList<ArtistId, ArtistId.Factory> similarArtists) {
			this.ArtistInfo = artistInfo;  this._ListID = listID; this._LookupTimestamp = lookupTimestamp;
			this._StatusCode = statusCode; this._SimilarArtists = similarArtists;
		}
		public static ArtistSimilarityListInfo CreateUnknown(ArtistInfo artist) {
			return new ArtistSimilarityListInfo(default(SimilarArtistsListId), artist, null, null, default(ArtistSimListStore));
		}
	}
}
