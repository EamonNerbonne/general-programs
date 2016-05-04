using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LastFMspider.LastFMSQLiteBackend;
using ArtistTopTracksStore = LastFMspider.ReachList<LastFMspider.LastFMSQLiteBackend.TrackId, LastFMspider.LastFMSQLiteBackend.TrackId.Factory>;

namespace LastFMspider {
	public struct ArtistTopTrack {
		public string Track;
		public long Reach;
	}
	public class ArtistTopTracksList {
		public DateTime LookupTimestamp;
		public string Artist;
		public ArtistTopTrack[] TopTracks;
		public int? StatusCode;

		internal static ArtistTopTracksList CreateErrorList(string artist, int errCode) {
			return new ArtistTopTracksList { Artist = artist, LookupTimestamp = DateTime.UtcNow, TopTracks = new ArtistTopTrack[0], StatusCode = errCode, };
		}
	}


	public struct ArtistTopTracksListInfo {//TODO: this should be a class?
		public readonly ArtistInfo ArtistInfo;

		readonly TopTracksListId _ListID;
		public TopTracksListId ListID {get{return _ListID;}}

		readonly DateTime? _LookupTimestamp;
		public DateTime? LookupTimestamp { get { return _LookupTimestamp; } }

		readonly ArtistTopTracksStore _TopTracks;
		public IEnumerable<HasReach<TrackId>> TopTracks { get { return _TopTracks.Rankings; } }

		readonly int? _StatusCode;
		public int? StatusCode { get { return _StatusCode; } }

		internal ArtistTopTracksListInfo(TopTracksListId listID, ArtistInfo artistInfo,  DateTime? lookupTimestamp,
			int? statusCode, ReachList<TrackId, TrackId.Factory> rankings) {
			this.ArtistInfo = artistInfo; this._ListID = listID; this._LookupTimestamp = lookupTimestamp;
			this._StatusCode = statusCode; this._TopTracks = rankings;
		}
		public static ArtistTopTracksListInfo CreateUnknown(ArtistInfo artistInfo) {
			return new ArtistTopTracksListInfo(default(TopTracksListId), artistInfo, null, null, default(ArtistTopTracksStore));
		}
		public bool IsKnown { get { return ListID.HasValue; } }
	}
}
