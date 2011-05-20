using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using LastFMspider;
using Newtonsoft.Json;
using SongDataLib;
using SongSearchSite.Code.Model;
using Ionic.Zip;

namespace SongSearchSite.Code.Handlers {
	public class BouncePlaylistHandler : IHttpHandler {
		public bool IsReusable { get { return true; } }

		public void ProcessRequest(HttpContext context) {
			var postedFile = context.Request.Files["playlist"];
			SongFileData[] playlistLocal = postedFile != null ? GetPlaylistFromM3U(context, postedFile) : GetPlaylistFromJson(context);

			PlaylistFormat format = (PlaylistFormat)Enum.Parse(typeof(PlaylistFormat), context.Request["format"]);

			if (format == PlaylistFormat.m3u || format == PlaylistFormat.m3u8)
				ProcessAsM3u(context, playlistLocal, format);
			else if (format == PlaylistFormat.zip)
				ProcessAsZip(context, playlistLocal);
			else if (format == PlaylistFormat.json)
				ProcessAsJson(context, playlistLocal);
			else throw new NotImplementedException(format.ToString());
		}

		private static SongFileData[] GetPlaylistFromJson(HttpContext context) {
			PlaylistEntry[] playlistFromJson = JsonConvert.DeserializeObject<PlaylistEntry[]>(context.Request["playlist"]);


			return playlistFromJson.Select(item => item.LookupBestGuess(context)).Where(item => item != null).ToArray();
		}

		private static SongFileData[] GetPlaylistFromM3U(HttpContext context, HttpPostedFile postedFile) {
			var m3uPlaylist = SongFileDataFactory.LoadExtM3U(postedFile.InputStream, Path.GetExtension(postedFile.FileName));
			return RepairPlaylist.GetPlaylistFixed(m3uPlaylist, SongDbContainer.FuzzySongSearcher, uri => SongDbContainer.GetSongFromFullUri(context, uri.ToString()))
				.OfType<SongFileData>().ToArray();
		}

		static void ProcessAsJson(HttpContext context, SongFileData[] playlistLocal) {
			try {
				context.Response.ContentType = "application/json";
				context.Response.ContentEncoding = Encoding.UTF8;

				Func<Uri, Uri> uriMapper = SongDbContainer.LocalSongPathToAppRelativeMapper(context);
				context.Response.Output.Write(
					JsonConvert.SerializeObject(playlistLocal.Select(song => PlaylistEntry.MakeEntry(uriMapper, song)).ToArray())
				);
			} catch (Exception e) {
				context.Response.ContentType = "application/json";
				context.Response.Output.Write(JsonConvert.SerializeObject(new SimilarPlaylistError { error = e.GetType().FullName, message = e.Message, fulltrace = e.ToString() }));
			}
		}

		static void ProcessAsM3u(HttpContext context, SongFileData[] playlistLocal, PlaylistFormat format) {
			context.Response.ContentType = "audio/x-mpegurl";
			context.Response.ContentEncoding = format == PlaylistFormat.m3u8 ? Encoding.UTF8 : Encoding.GetEncoding(1252);
			context.Response.Headers["Content-Disposition"] = "attachment; filename=" + Uri.EscapeDataString(context.User.Identity.Name + DateTime.Now.ToString("yyyyddMM") + "." + format);

			var uriMapper = SongDbContainer.LocalSongPathToAbsolutePathMapper(context);
			context.Response.Write(MakeM3u(playlistLocal, songdata => uriMapper(songdata.SongUri).AbsoluteUri));
		}
		static void ProcessAsZip(HttpContext context, SongFileData[] playlistLocal) {
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

		private static string MakeM3u(SongFileData[] playlistLocal, Func<ISongFileData, string> songToPathMapper) {
			using (var writer = new StringWriter()) {
				SongFileDataFactory.WriteSongsToM3U(writer, playlistLocal, songToPathMapper);
				return writer.ToString();
			}
		}
	}
}