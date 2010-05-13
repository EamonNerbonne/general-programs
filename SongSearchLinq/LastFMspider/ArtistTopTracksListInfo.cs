using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LastFMspider.LastFMSQLiteBackend;

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
		public readonly TopTracksListId ListID;
		public readonly DateTime? LookupTimestamp;
		readonly ReachList<TrackId, TrackId.Factory> _TopTracks;
		public IEnumerable<HasReach<TrackId>> TopTracks { get { return _TopTracks.Rankings; } }
		public readonly int? StatusCode;
		internal ArtistTopTracksListInfo(TopTracksListId listID, ArtistInfo artistInfo,  DateTime? lookupTimestamp,
			int? statusCode, ReachList<TrackId, TrackId.Factory> rankings) {
			this.ArtistInfo = artistInfo; this.ListID = listID; this.LookupTimestamp = lookupTimestamp;
			this.StatusCode = statusCode; this._TopTracks = rankings;
		}
		public static ArtistTopTracksListInfo CreateUnknown(ArtistInfo artistInfo) {
			return new ArtistTopTracksListInfo(default(TopTracksListId), artistInfo, null, null, new ReachList<TrackId, TrackId.Factory>(new byte[] { }));
		}
		public bool IsKnown { get { return ListID.HasValue; } }
	}
}
