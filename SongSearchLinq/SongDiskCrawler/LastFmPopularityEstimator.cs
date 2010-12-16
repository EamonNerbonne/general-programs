using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using EmnExtensions.Algorithms;
using EmnExtensions.Text;
using LastFMspider;
using SongDataLib;

namespace SongDiskCrawler {
	class LastFmPopularityEstimator : IPopularityEstimator {
		readonly ConcurrentDictionary<string, ArtistTopTracksList> toptracksCache = new ConcurrentDictionary<string, ArtistTopTracksList>();
		readonly SongTools tools;
		public LastFmPopularityEstimator(SongTools tools) { this.tools = tools; }
		public Popularity EstimatePopularity(string artist, string title) {
			if (artist == null || title == null) return default(Popularity);
			var song = SongRef.Create(artist, title);
			var canonicalArtist = song.GetLowerArtist();
			var canonicalTrack = title.ToLatinLowercase();
			ArtistTopTracksList toptracks =
			toptracksCache.GetOrAdd(canonicalArtist, a => {
				var toptracksFromLfm = tools.SimilarSongs.LookupTopTracks(artist);
				for (int i = 0; i < toptracksFromLfm.TopTracks.Length; i++)
					toptracksFromLfm.TopTracks[i].Track = toptracksFromLfm.TopTracks[i].Track.ToLatinLowercase();//canonicalization to speed up lookup below.
				return toptracksFromLfm;
			});

			var q =
				(from toptrack in toptracks.TopTracks
				 let distance = (canonicalTrack.LevenshteinDistanceScaled(toptrack.Track) +
					 canonicalTrack.CanonicalizeBasic().LevenshteinDistanceScaled(toptrack.Track.CanonicalizeBasic())) *
					 (canonicalTrack.Length + toptrack.Track.Length + 40)
				 where distance < 60
				 orderby distance / toptrack.Reach
				 select new { track = toptrack, cost = distance }).ToArray();
			Popularity retval = new Popularity();
			if (toptracks.TopTracks.Length > 0)
				retval.ArtistPopularity = (int)toptracks.TopTracks[0].Reach;

			if (q.Length > 0)
				retval.TitlePopularity = (int)(q[0].track.Reach * (1.0 - (q[0].cost / 120.0)));
			return retval;
		}
	}
}
