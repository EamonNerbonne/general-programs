using System;
using System.Web;
using Newtonsoft.Json;
using System.Linq;
using LastFMspider;
using SongDataLib;
using System.Collections.Generic;
using System.Diagnostics;

namespace SongSearchSite {
	public class SimilarPlaylist : IHttpHandler {
		public bool IsReusable { get { return true; } }

		class PlaylistEntryJson {
			// ReSharper disable UnaccessedField.Local
			public string label, href;
			public int? length;
			public double? replaygain;
			public int? rating;
			// ReSharper restore UnaccessedField.Local
		}

		public void ProcessRequest(HttpContext context) {
			try {
				PlaylistEntryJson[] playlistFromJson = JsonConvert.DeserializeObject<PlaylistEntryJson[]>(context.Request["playlist"]);
				var playlistSongNames =
					from entry in playlistFromJson
					let path = Uri.UnescapeDataString(entry.href)
					let songdata = SongDbContainer.GetSongFromFullUri(path) as SongFileData
					where songdata != null
					select SongRef.Create(songdata);

				var timer = Stopwatch.StartNew();
				var res =
					FindSimilarPlaylist.ProcessPlaylist(
						tools: SongDbContainer.LastFmTools,
						seedSongs: playlistSongNames,
						MaxSuggestionLookupCount: 1000,
						SuggestionCountTarget: 50,
						fuzzySearch: SongDbContainer.FuzzySongSearcher.FindBestMatch,
						shouldAbort: count => !context.Response.IsClientConnected || (timer.Elapsed.TotalMilliseconds + count * 350 > 20000));

				if (!context.Response.IsClientConnected)
					return;

				var uriMapper = SongDbContainer.LocalSongPathToAppRelativeMapper(context);

				PlaylistEntryJson[] knownForJson =
					(from knownSong in res.knownTracks
					 let mime = SongServeRequestProcessor.guessMIME(knownSong)
					 where mime == SongServeRequestProcessor.MIME_MP3 || mime == SongServeRequestProcessor.MIME_OGG
					 select new PlaylistEntryJson {
						 href = uriMapper(knownSong.SongUri).ToString(),
						 label = knownSong.HumanLabel,
						 length = knownSong.Length,
						 replaygain = knownSong.track_gain,
						 rating = knownSong.rating
					 }).ToArray();
				string[] unknownForJson =
					(from unknownSong in res.unknownTracks
					 select unknownSong.ToString())
					 .Concat(from knownSong in res.knownTracks
							 let mime = SongServeRequestProcessor.guessMIME(knownSong)
							 where mime != SongServeRequestProcessor.MIME_MP3 && mime != SongServeRequestProcessor.MIME_OGG
							 select knownSong.HumanLabel).Take(50).ToArray();

				context.Response.ContentType = "application/json";
				context.Response.Output.Write(
					JsonConvert.SerializeObject(
						new Dictionary<string, object> {
						{ "known", knownForJson},
						{"unknown", unknownForJson},
						{"lookups", res.LookupsDone},
						{"weblookups", res.LookupsWebTotal},
						{"milliseconds",timer.ElapsedMilliseconds},
					}
					)
				);
			} catch (Exception e) {
				context.Response.ContentType = "application/json";
				context.Response.Output.Write(
					JsonConvert.SerializeObject(
						new Dictionary<string, object> {
						{ "error",e.GetType().FullName},
						{"message",e.Message},
						{"fulltrace",e.ToString()},
					})
				);
			}
		}
	}
}
