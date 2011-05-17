using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using SongDataLib;
using SongSearchSite.Code.Model;
using Ionic.Zip;

namespace SongSearchSite.Code.Handlers {
	public class BouncePlaylistHandler : IHttpHandler {
		public bool IsReusable { get { return true; } }

		public void ProcessRequest(HttpContext context) {
			PlaylistEntry[] playlistFromJson = JsonConvert.DeserializeObject<PlaylistEntry[]>(context.Request["playlist"]);

			ISongFileData[] playlistLocal = playlistFromJson.Select(item => item.LookupBestGuess()).Where(item => item != null).ToArray();

			PlaylistFormat format = (PlaylistFormat)Enum.Parse(typeof(PlaylistFormat), context.Request["format"]);

			if (format == PlaylistFormat.m3u)
				ProcessAsM3u(context, playlistLocal);
			else if (format == PlaylistFormat.zip)
				ProcessAsZip(context, playlistLocal);
			else throw new NotImplementedException(format.ToString());


			//TODO:support other formats, refactor m3u support to separate file. 
		}

		static void ProcessAsM3u(HttpContext context, ISongFileData[] playlistLocal) {
			context.Response.ContentType = "audio/x-mpegurl";
			context.Response.ContentEncoding = Encoding.GetEncoding(1252);
			context.Response.Headers["Content-Disposition"] = "attachment; filename=" + Uri.EscapeDataString(context.User.Identity.Name + DateTime.Now.ToString("yyyyddMM") + ".m3u");

			var uriMapper = SongDbContainer.LocalSongPathToAbsolutePathMapper(context);
			context.Response.Write(MakeM3u(playlistLocal, songdata => uriMapper(songdata.SongUri).AbsoluteUri));
		}
		static void ProcessAsZip(HttpContext context, ISongFileData[] playlistLocal) {
			string filename = context.User.Identity.Name + DateTime.Now.ToString("yyyyddMM");
			context.Response.ContentType = "application/zip";
			context.Response.AddHeader("Content-Disposition", "attachment; filename=" + filename + ".zip");
			context.Response.BufferOutput = false;
			var m3ustring = MakeM3u(playlistLocal, songdata => filename + @"\" + Path.GetFileName(songdata.SongUri.LocalPath));
			using (ZipFile zip = new ZipFile()) {
				zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestSpeed;
				zip.AddFiles(playlistLocal.Select(localSong => localSong.SongUri.LocalPath), false, filename);

				zip.AddEntry(filename + ".m3u", Encoding.GetEncoding(1252).GetBytes(m3ustring));
				zip.AddEntry(filename + ".m3u8", Encoding.UTF8.GetBytes(m3ustring));
				zip.Save(context.Response.OutputStream);
			}
		}

		private static string MakeM3u(ISongFileData[] playlistLocal, Func<ISongFileData, string> songToPathMapper) {
			StringBuilder m3u = new StringBuilder();
			m3u.Append("#EXTM3U\n");
			foreach (ISongFileData songdata in playlistLocal)
				m3u.Append("#EXTINF:" + songdata.Length + "," + songdata.HumanLabel + "\n" + songToPathMapper(songdata) + "\n");
			return m3u.ToString();
		}
	}
}