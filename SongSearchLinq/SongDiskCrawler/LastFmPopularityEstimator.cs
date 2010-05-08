using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using LastFMspider;
using LastFMspider.LastFMSQLiteBackend;
using EmnExtensions.Algorithms;
using EmnExtensions.Text;

namespace SongDiskCrawler {
	class LastFmPopularityEstimator : IPopularityEstimator {
		Dictionary<string, ArtistTopTracksList> toptracksCache = new Dictionary<string, ArtistTopTracksList>();
		LastFmTools tools;
		public LastFmPopularityEstimator(LastFmTools tools) { this.tools = tools; }
		public Popularity EstimatePopularity(string artist, string track) {
			var song = SongRef.Create(artist, track);
			var canonicalArtist = song.GetLowerArtist();
			var canonicalTrack = track.ToLatinLowercase();
			ArtistTopTracksList toptracks;
			if (!toptracksCache.TryGetValue(canonicalArtist, out toptracks)) {
				 toptracks = tools.SimilarSongs.LookupTopTracks(artist);
				for(int i=0;i<toptracks.TopTracks.Length;i++)
					toptracks.TopTracks[i].Track = toptracks.TopTracks[i].Track.ToLatinLowercase();//canonicalization to speed up lookup below.
				toptracksCache[canonicalArtist] = toptracks;	
			}

			var q =
				(from toptrack in toptracks.TopTracks
				let distance = (canonicalTrack.LevenshteinDistanceScaled (toptrack.Track) +
					canonicalTrack.CanonicalizeBasic().LevenshteinDistanceScaled(toptrack.Track.CanonicalizeBasic())) *
					(canonicalTrack.Length + toptrack.Track.Length + 40)
				where distance < 60
				orderby  distance/toptrack.Reach
				select new { track = toptrack, cost = distance }).ToArray();
			Popularity retval = new Popularity(); 
			if (toptracks.TopTracks.Length > 0)
				retval.ArtistPopularity = (int) toptracks.TopTracks[0].Reach;

			if (q.Length > 0)
				retval.TitlePopularity = (int)(q[0].track.Reach * (1.0-(q[0].cost/120.0)));
			return retval;
		}
	}
}
