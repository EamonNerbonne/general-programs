using System;
using System.Web;
using Newtonsoft.Json;
using System.Linq;
using LastFMspider;
using SongDataLib;
using System.Collections.Generic;

namespace SongSearchSite {
	public class SimilarPlaylist : IHttpHandler {
		public bool IsReusable { get { return true; } }

		class PlaylistEntryJson {
			public string label = null, href = null;
			public int length = 0;
		}

		public void ProcessRequest(HttpContext context) {

			PlaylistEntryJson[] playlistFromJson = JsonConvert.DeserializeObject<PlaylistEntryJson[]>(context.Request["playlist"]);
			List<SongData> songs = (
				from entry in playlistFromJson
				let uri = new Uri(entry.href)
				let path = Uri.UnescapeDataString( VirtualPathUtility.ToAppRelative(uri.AbsolutePath))
				let songdata = SongDbContainer.GetSongFromFullUri(path) as SongData
				where songdata != null
				select songdata).ToList();

			List<SongRef> unknownSongs = new List<SongRef>();

			var res = FindSimilarPlaylist.ProcessPlaylist(SongDbContainer.LastFmTools,
				// sr=>null,
				SongDbContainer.FuzzySongSearcher.FindBestMatch,
				 songs, unknownSongs, Math.Min(Math.Max(10,songs.Count * 2), 100), 50);

			string serverName = context.Request.Headers["Host"];
			string appName = context.Request.ApplicationPath;
			if (appName == "/")
				appName = "";
			string urlprefix = "http://" + serverName + appName + "/songs/";

			PlaylistEntryJson[] knownForJson =
				(from knownSong in res.knownTracks
				 select new PlaylistEntryJson {
					 href = urlprefix +SongDbContainer.NormalizeSongPath(knownSong),
					 label = knownSong.HumanLabel,
					 length = knownSong.Length
				 }).ToArray();
			string[] unknownForJson =
				(from unknownSong in res.unknownTracks
				 select unknownSong.ToString()).ToArray();

			context.Response.ContentType = "application/json";
			context.Response.Output.Write(
				JsonConvert.SerializeObject(
					new Dictionary<string, object> {
						{ "known",knownForJson},
						{"unknown",unknownForJson}
					}
				)
			);
			//write your handler implementation here.
		}
	}
}
