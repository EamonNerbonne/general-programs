using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SongDataLib;
using LastFMspider;

namespace SongSearchSite.Code.Model {
	public class PlaylistEntry {
		// ReSharper disable UnaccessedField.Local
		// ReSharper disable UnaccessedField.Global
		// ReSharper disable MemberCanBePrivate.Global
		public string href, artist, title, label;
		public int? length;
		public double? replaygain;
		public int? rating;
		public double? popA, popT;
		// ReSharper enable UnaccessedField.Local
		// ReSharper enable UnaccessedField.Global
		// ReSharper enable MemberCanBePrivate.Global

		public ISongFileData LookupLocalData() { return SongDbContainer.GetSongFromFullUri(Uri.UnescapeDataString(href)); }
		public ISongFileData LookupBestGuess() {
			var match = SongDbContainer.GetSongFromFullUri(Uri.UnescapeDataString(href));
			if (match != null) return match;
			if (artist != null && title != null)
				return SongDbContainer.FuzzySongSearcher.FindBestMatch(SongRef.Create(artist, title)).Song;
			return (
				from songref in SongRef.PossibleSongRefs(label)
				let pmatch = SongDbContainer.FuzzySongSearcher.FindBestMatch(songref)
				where pmatch.GoodEnough
				orderby pmatch.Cost
				select pmatch.Song).FirstOrDefault();
		}

		public static PlaylistEntry MakeEntry(Func<Uri, Uri> uriMapper, SongFileData knownSong) {
			return new PlaylistEntry {
				href = uriMapper(knownSong.SongUri).ToString(),
				label = knownSong.HumanLabel,
				length = knownSong.Length,
				replaygain = knownSong.track_gain,
				rating = knownSong.rating,
				popA = knownSong.popA_forscripting,
				popT = knownSong.popT_forscripting
			};
		}

	}
	enum PlaylistFormat {
		m3u, m3u8, xml, json, zip
	}
}