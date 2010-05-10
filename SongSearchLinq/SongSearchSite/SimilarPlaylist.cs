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
		static object syncroot = new object();
		public void ProcessRequest(HttpContext context) {

			PlaylistEntryJson[] playlistFromJson = JsonConvert.DeserializeObject<PlaylistEntryJson[]>(context.Request["playlist"]);
			List<SongData> songs = (
				from entry in playlistFromJson
				let path = Uri.UnescapeDataString(entry.href)
				let songdata = SongDbContainer.GetSongFromFullUri(path) as SongData
				where songdata != null
				select songdata).ToList();

			List<SongRef> unknownSongs = new List<SongRef>();
			FindSimilarPlaylist.SimilarPlaylistResults res=null;
			lock (syncroot) {
				if (!context.Response.IsClientConnected)
					return;
				res = FindSimilarPlaylist.ProcessPlaylist(SongDbContainer.LastFmTools,
					// sr=>null,
					SongDbContainer.FuzzySongSearcher.FindBestMatch,
					 songs, unknownSongs, 100, 40);
			}

			var uriMapper = SongDbContainer.LocalSongPathToAppRelativeMapper(context);

			PlaylistEntryJson[] knownForJson =
				(from knownSong in res.knownTracks
				 select new PlaylistEntryJson {
					 href = uriMapper(knownSong.SongUri).ToString(),
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
