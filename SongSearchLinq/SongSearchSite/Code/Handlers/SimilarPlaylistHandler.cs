using System;
using System.Web;
using Newtonsoft.Json;
using System.Linq;
using LastFMspider;
using SongDataLib;
using System.Collections.Generic;
using System.Diagnostics;
using SongSearchSite.Code.Model;

namespace SongSearchSite.Code.Handlers {
	public class SimilarPlaylistHandler : IHttpHandler {
		public bool IsReusable { get { return true; } }
		public void ProcessRequest(HttpContext context) {
			try {
				PlaylistEntry[] playlistFromJson = JsonConvert.DeserializeObject<PlaylistEntry[]>(context.Request["playlist"]);
				var playlistSongs =
					from entry in playlistFromJson
					let songdata = entry.LookupBestGuess(SongDbContainer.AppBaseUri(context)) as SongFileData
					where songdata != null
					//from songref in songdata.PossibleSongs
					select songdata;

				double lookupDur;
				var timer = Stopwatch.StartNew();
				var res =
					FindSimilarPlaylist.ProcessPlaylist(
						 SongDbContainer.LastFmTools,
						 SongDbContainer.MemoizedFuzzyTrackLookup,
						playlistSongs,
						new SongRef[]{},
						out lookupDur,
						MaxSuggestionLookupCount: 1000,
						SuggestionCountTarget: 50,
						shouldAbort: count => !context.Response.IsClientConnected || (timer.Elapsed.TotalMilliseconds + count * 350 > 20000));

				if (!context.Response.IsClientConnected)
					return;

				var uriMapper = SongDbContainer.LocalSongPathToAppRelativeMapper(context);

				PlaylistEntry[] knownForJson =
					(from knownSong in res.knownTracks
					 let mime = SongServeRequestProcessor.guessMIME(knownSong)
					 where mime == SongServeRequestProcessor.MIME_MP3 || mime == SongServeRequestProcessor.MIME_OGG
					 select PlaylistEntry.MakeEntry(uriMapper, knownSong)).ToArray();
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
						new SimilarPlaylist {
							known = knownForJson,
							unknown = unknownForJson,
							lookups = res.LookupsDone,
							weblookups = res.LookupsWebTotal,
							lookuperrs = res.SqlErrorsTotal,
							milliseconds = (int)timer.ElapsedMilliseconds,
							msSimDb = (int)(lookupDur+0.5)
						}
					)
				);
			} catch (Exception e) {
				context.Response.ContentType = "application/json";
				context.Response.Output.Write(
					JsonConvert.SerializeObject(
						new SimilarPlaylistError { error = e.GetType().FullName, message = e.Message, fulltrace = e.ToString() })
				);
			}
		}

	}
}
