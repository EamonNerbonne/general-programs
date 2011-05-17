using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using SongDataLib;
using SongSearchSite.Code.Model;

namespace SongSearchSite.Code {
	public class PlaylistBouncer : IHttpHandler {
		public bool IsReusable { get { return true; } }

		public void ProcessRequest(HttpContext context) {
			PlaylistEntry[] playlistFromJson = JsonConvert.DeserializeObject<PlaylistEntry[]>(context.Request["playlist"]);
			PlaylistFormat format = (PlaylistFormat)Enum.Parse(typeof(PlaylistFormat), context.Request["format"]);

			ISongFileData[] playlistLocal = playlistFromJson.Select(item => item.LookupBestGuess()).ToArray();

		}
	}
}