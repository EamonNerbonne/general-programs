﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
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

		public SongFileData LookupLocalData(HttpContext context) { return SongDbContainer.GetSongFromFullUri(SongDbContainer.AppBaseUri(context), href); }
		public SongFileData LookupBestGuess(Uri appBaseUri) {
			var match = SongDbContainer.GetSongFromFullUri(appBaseUri, href);
			if (match != null) return match;
			Uri songuri;
			try {
				songuri = new Uri(appBaseUri, href);
			} catch (Exception e) {
				throw new Exception("href:" + href+" base: "+appBaseUri, e);
			}
			var externalSongFile = label != null && artist == null
				? (MinimalSongFileData)new PartialSongFileData(appBaseUri, songuri, label, length)
				: new SongFileData(appBaseUri, songuri, artist, title, length ?? 0, rating, replaygain);
			return RepairPlaylist.FindBestSufficientMatch(SongDbContainer.FuzzySongSearcher, externalSongFile);
		}

		public static PlaylistEntry MakeEntry(Func<Uri, Uri> uriMapper, SongFileData knownSong) {
			return new PlaylistEntry {
				href = uriMapper(knownSong.SongUri).ToString(),
				label = knownSong.artist == null || knownSong.title == null ? knownSong.HumanLabel : null,
				artist = knownSong.artist,
				title = knownSong.title,
				length = knownSong.Length,
				replaygain = knownSong.track_gain,
				rating = knownSong.rating,
				popA = knownSong.popA_forscripting,
				popT = knownSong.popT_forscripting,
			};
		}
	}

	public static class PlaylistHelpers {

		public static PlaylistEntry[] ParsePlaylistFromJson(string jsonPlaylist) { return JsonConvert.DeserializeObject<PlaylistEntry[]>(jsonPlaylist); }
		public static SongFileData[] CleanedPlaylistFromJson(HttpContext context, string jsonPlaylist) {
			Uri appBaseUri = SongDbContainer.AppBaseUri(context);
			return ParsePlaylistFromJson(jsonPlaylist).Select(item => item.LookupBestGuess(appBaseUri)).Where(item => item != null).ToArray();
		}

		public static string SerializeToJson(HttpContext context, SongFileData[] playlistLocal) {
			Func<Uri, Uri> uriMapper = SongDbContainer.LocalSongPathToAppRelativeMapper(context);
			return JsonConvert.SerializeObject(playlistLocal.Select(song => PlaylistEntry.MakeEntry(uriMapper, song)).ToArray());
		}

		public static string CleanupJsonPlaylist(HttpContext context, string jsonPlaylist) {
			return SerializeToJson(context, CleanedPlaylistFromJson(context, jsonPlaylist));
		}
	}
	enum PlaylistFormat {
		m3u, m3u8, xml, json, zip
	}
}