using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using EmnExtensions.Text;
using HttpHeaderHelper;
using SongDataLib;
using System.Xml.Linq;

namespace SongSearchSite {
	public class PlaylistRequestProcessor : IHttpRequestProcessor {
		HttpRequestHelper helper;
		public PlaylistRequestProcessor(HttpRequestHelper helper) { this.helper = helper; }
		public void ProcessingStart() { }
		static DateTime startupUtc = DateTime.UtcNow;

		bool isXml, includeRemote, extm3u, coreAttrsOnly, viewXslt;
		int? topLimit;
		Encoding enc;
		string searchQuery;
		public PotentialResourceInfo DetermineResource() {
			isXml = false;
			HttpContext context = helper.Context;
			string extension = VirtualPathUtility.GetExtension(context.Request.AppRelativeCurrentExecutionFilePath).ToLowerInvariant();
			ResourceInfo res = new ResourceInfo();

			if (context.Request["debug"] == "true") {
				res.MimeType = "text/plain";
				enc = Encoding.UTF8;
			} else if (extension == ".m3u8") {
				res.MimeType = "application/octet-stream";
				enc = Encoding.UTF8;
			} else if (extension == ".m3u") {
				enc = Encoding.GetEncoding(1252);

				res.MimeType = "audio/x-mpegurl";
			} else if (extension == ".xml") {
				enc = Encoding.UTF8;
				res.MimeType = "text/xml";
				isXml = true;
			}


			includeRemote = context.Request.QueryString["remote"] == "allow";
			extm3u = context.Request.QueryString["extm3u"] != "false";
			coreAttrsOnly = context.Request.QueryString["fulldata"] != "true";
			topLimit = context.Request.QueryString["top"].ParseAsInt32();
			viewXslt = context.Request.QueryString["view"] == "xslt";

			var path = context.Request.AppRelativeCurrentExecutionFilePath.Split('/');
			var searchterms = from pathpart in path.Skip(1).Take(path.Length - 2) select pathpart;
			var queryParam = context.Request.QueryString["q"] ?? "";
			if (queryParam.IndexOf((char)0xfffd) != -1) {//0xfffd is the "error" code, if it contains that, it's typically a sign that the request isn't UTF-8 as it should be, so let's try Latin1~=Windows1252....
				context.Request.ContentEncoding = Encoding.GetEncoding(1252);
				queryParam = context.Request.QueryString["q"] ?? "";
			}
			searchterms = searchterms.Concat(queryParam.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
			searchterms = searchterms.Select(s => Canonicalize.Basic(s)).ToArray();
			searchQuery = string.Join(" ", searchterms.ToArray());

			if (extension == ".m3u" || extension == ".m3u8")
				context.Response.Headers["Content-Disposition"] = "attachment; filename=" + Uri.EscapeDataString("playlist_" + searchQuery + extension); //searchquery has been canonicalized: no dangerous injection possible.

			res.ETag = ResourceInfo.GenerateETagFrom(searchQuery, includeRemote, extm3u, isXml, coreAttrsOnly, extension, topLimit, startupUtc, viewXslt);
			res.ResourceLength = null;//unknown
			res.TimeStamp = startupUtc;
			Console.WriteLine("Request Determined: [" + (isXml ? 'X' : ' ') + (includeRemote ? 'R' : ' ') + (enc == Encoding.UTF8 ? 'U' : ' ') + (extm3u ? 'E' : ' ') + "] q=" + searchQuery);
			return res;
		}

		public DateTime? DetermineExpiryDate() {
			return null;//DateTime.UtcNow.AddHours(1);
		}

		public bool SupportRangeRequests {
			get { return false; }
		}

		public void WriteByteRange(Range range) { throw new NotImplementedException(); }

		public void WriteEntireContent() {
			HttpContext context = helper.Context;

			if (extm3u && !isXml)
				context.Response.Write("#EXTM3U\n");
			string serverName = context.Request.Headers["Host"];
			string appName = context.Request.ApplicationPath;

			if (appName == "/")
				appName = "";
			string songsprefix = "http://" + serverName + appName + "/songs/";
			Uri songsAbsolute = new Uri(songsprefix);
			Uri currentUrl = context.Request.Url;

			//			urlprefix = "http://home.nerbonne.org/";


			var searchResults = SongDbContainer.SearchableSongDB.Search(searchQuery);
			if (coreAttrsOnly)
				searchResults = searchResults.Where(song => { var mime = SongServeRequestProcessor.guessMIME(song); return mime == SongServeRequestProcessor.MIME_MP3 || mime == SongServeRequestProcessor.MIME_OGG; });
			if (!includeRemote)
				searchResults = searchResults.Where(song => song.IsLocal);
			if (topLimit.HasValue)
				searchResults = searchResults.Take(topLimit.Value);

			if (isXml) {

				var uriMapper = coreAttrsOnly ? SongDbContainer.LocalSongPathToAppRelativeMapper(context)
					: SongDbContainer.LocalSongPathToAbsolutePathMapper(context);
				new XDocument(
					viewXslt ? new XProcessingInstruction("xml-stylesheet", "type=\"text/xsl\"  href=\"searchresult.xsl\"") : null,
					new XElement("songs",
						new XAttribute("base", currentUrl),
						from s in searchResults
						select s.ConvertToXml(uri => uriMapper(uri).ToString(), coreAttrsOnly)
					)
				).Save(context.Response.Output);
			} else {
				var uriMapper = SongDbContainer.LocalSongPathToAbsolutePathMapper(context);
				foreach (ISongData songdata in searchResults)
					context.Response.Write(makeM3UEntry(songdata, extm3u, uriMapper));
			}
		}

		static string makeM3UEntry(ISongData song, bool extm3u, Func<Uri, Uri> makeAbsolute) {
			string url = makeAbsolute(song.SongUri).ToString();
			if (extm3u)
				return "#EXTINF:" + song.Length + "," + song.HumanLabel + "\n" + url + "\n";
			else
				return url + "?" + HttpUtility.UrlEncode(song.HumanLabel) + "\n";
		}
	}

	public class PlaylistHandler : IHttpHandler {

		public bool IsReusable { get { return false; } }

		public void ProcessRequest(HttpContext context) {
			Console.WriteLine("PlaylistHandler called.");
			HttpRequestHelper helper = new HttpRequestHelper(context);
			PlaylistRequestProcessor proc = new PlaylistRequestProcessor(helper);
			helper.Process(proc);
		}
	}
}