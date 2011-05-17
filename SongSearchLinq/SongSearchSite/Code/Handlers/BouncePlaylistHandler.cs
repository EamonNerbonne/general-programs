using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using SongDataLib;
using SongSearchSite.Code.Model;
using HttpHeaderHelper;

namespace SongSearchSite.Code.Handlers {
	public class BouncePlaylistHandler : IHttpHandler {
		public bool IsReusable { get { return true; } }

		public void ProcessRequest(HttpContext context) {
			PlaylistEntry[] playlistFromJson = JsonConvert.DeserializeObject<PlaylistEntry[]>(context.Request["playlist"]);

			ISongFileData[] playlistLocal = playlistFromJson.Select(item => item.LookupBestGuess()).Where(item => item != null).ToArray();

			PlaylistFormat format = (PlaylistFormat)Enum.Parse(typeof(PlaylistFormat), context.Request["format"]);
			if (format != PlaylistFormat.m3u)
				throw new NotImplementedException(format.ToString());

			if (format == PlaylistFormat.m3u)
				ProcessAsM3u(context, playlistLocal);
			else throw new NotImplementedException(format.ToString());


			//TODO:support other formats, refactor m3u support to separate file. 
		}

		static void ProcessAsM3u(HttpContext context, ISongFileData[] playlistLocal)
		{
			context.Response.ContentType = "audio/x-mpegurl";
			context.Response.ContentEncoding = Encoding.GetEncoding(1252);
			context.Response.Headers["Content-Disposition"] = "attachment; filename=" + Uri.EscapeDataString(context.User.Identity.Name + DateTime.Now.ToString("yyyyddMM") + ".m3u");

			context.Response.Write("#EXTM3U\n");
			var uriMapper = SongDbContainer.LocalSongPathToAbsolutePathMapper(context);
			foreach (ISongFileData songdata in playlistLocal)
				context.Response.Write("#EXTINF:" + songdata.Length + "," + songdata.HumanLabel + "\n" + uriMapper(songdata.SongUri).AbsoluteUri + "\n");
		}
	}

}