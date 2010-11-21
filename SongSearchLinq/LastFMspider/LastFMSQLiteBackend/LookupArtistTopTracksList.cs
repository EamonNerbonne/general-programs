using System.Linq;

namespace LastFMspider.LastFMSQLiteBackend {
	public class LookupArtistTopTracksList : AbstractLfmCacheOperation {
		public LookupArtistTopTracksList(LastFMSQLiteCache lfmCache) : base(lfmCache) { }

		public ArtistTopTracksList Execute(ArtistTopTracksListInfo artistTTinfo) {
			return DoInLockedTransaction(() => {
				var rankings =
					from toptrack in artistTTinfo.TopTracks
					let track = lfmCache.LookupTrack.Execute(toptrack.ForId)
					where track != null
					select new ArtistTopTrack {
						Reach = toptrack.Reach,
						Track = track.Title,
					};

				return new ArtistTopTracksList {
					TopTracks = rankings.ToArray(),
					Artist = artistTTinfo.ArtistInfo.Artist,
					LookupTimestamp = artistTTinfo.LookupTimestamp.Value.ToUniversalTime(),
					StatusCode = artistTTinfo.StatusCode,
				};
			});
		}
	}
}
