using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using EmnExtensions.Text;
using HttpHeaderHelper;
using LastFMspider;
using SongDataLib;
using System.Xml.Linq;

namespace SongSearchSite.Code.Handlers {

	public class SearchRequestHandler : IHttpHandler {
		public bool IsReusable { get { return false; } }
		public void ProcessRequest(HttpContext context) {
			Console.WriteLine("PlaylistHandler called.");
			HttpRequestHelper helper = new HttpRequestHelper(context);
			SearchRequestProcessor proc = new SearchRequestProcessor(helper);
			helper.Process(proc);
		}
	}

	public class SearchRequestProcessor : IHttpRequestProcessor {
		readonly HttpRequestHelper helper;
		public SearchRequestProcessor(HttpRequestHelper helper) { this.helper = helper; }
		public void ProcessingStart() { }

		bool isXml, includeRemote, extm3u, coreAttrsOnly, viewXslt, avoidDuplicates;
		SortOrdering orderby;
		int? topLimit;
		Encoding enc;
		string searchQuery;
		public PotentialResourceInfo DetermineResource() {
			DateTime startupUtc = SongDbContainer.LoadTime;
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
			context.Response.ContentEncoding = enc;



			includeRemote = context.Request.QueryString["remote"] == "allow";
			extm3u = context.Request.QueryString["extm3u"] != "false";
			coreAttrsOnly = context.Request.QueryString["fulldata"] != "true";
			topLimit = context.Request.QueryString["top"].ParseAsInt32();
			viewXslt = context.Request.QueryString["view"] == "xslt";
			avoidDuplicates = context.Request.QueryString["nodup"] == "on";
			orderby = SortOrdering.Parse(context.Request.QueryString["ordering"]).Append((int)SongColumn.Rating);

			var path = context.Request.AppRelativeCurrentExecutionFilePath.Split('/');
			var searchterms = from pathpart in path.Skip(1).Take(path.Length - 2) select pathpart;
			var queryParam = context.Request.QueryString["q"] ?? "";
			if (queryParam.IndexOf((char)0xfffd) != -1) {//0xfffd is the "error" code, if it contains that, it's typically a sign that the request isn't UTF-8 as it should be, so let's try Latin1~=Windows1252....
				context.Request.ContentEncoding = Encoding.GetEncoding(1252);
				queryParam = context.Request.QueryString["q"] ?? "";
			}
			searchterms = searchterms.Concat(queryParam.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
			searchterms = searchterms.Select(Canonicalize.Basic).ToArray();
			searchQuery = string.Join(" ", searchterms.ToArray());

			if (extension == ".m3u" || extension == ".m3u8")
				context.Response.Headers["Content-Disposition"] = "attachment; filename=" + Uri.EscapeDataString("playlist_" + searchQuery + extension); //searchquery has been canonicalized: no dangerous injection possible.

			res.ETag = ResourceInfo.GenerateETagFrom(searchQuery, includeRemote, extm3u, isXml, coreAttrsOnly, extension, topLimit, startupUtc, viewXslt, orderby, avoidDuplicates);
			res.ResourceLength = null;//unknown
			res.TimeStamp = startupUtc;
			//Console.WriteLine("Request Determined: [" + (isXml ? 'X' : ' ') + (includeRemote ? 'R' : ' ') + (enc == Encoding.UTF8 ? 'U' : ' ') + (extm3u ? 'E' : ' ') + "] q=" + searchQuery);
			return res;
		}

		public DateTime? DetermineExpiryDate() { return null; }//DateTime.UtcNow.AddHours(1);

		public bool SupportRangeRequests { get { return false; } }

		public void WriteByteRange(Range range) { throw new NotImplementedException(); }

		public void WriteEntireContent() {
			HttpContext context = helper.Context;

			if (extm3u && !isXml)
				context.Response.Write("#EXTM3U\n");
			//string serverName = context.Request.Headers["Host"];
			//string appName = context.Request.ApplicationPath;
			//if (appName == "/") appName = "";
			//string songsprefix = "http://" + serverName + appName + "/songs/";
			//Uri songsAbsolute = new Uri(songsprefix);

			Uri currentUrl = context.Request.Url;

			//			urlprefix = "http://home.nerbonne.org/";


			var searchResults = SongDbContainer.SearchableSongDB.Search(searchQuery, SongDbContainer.RankMapFor(orderby));
			if (coreAttrsOnly)
				searchResults = searchResults.Where(song => { var mime = SongServeRequestProcessor.guessMIME(song); return mime == SongServeRequestProcessor.MIME_MP3 || mime == SongServeRequestProcessor.MIME_OGG; });
			if (!includeRemote)
				searchResults = searchResults.Where(song => song.IsLocal);
			if (avoidDuplicates) {
				Dictionary<string, List<int>> seen = new Dictionary<string, List<int>>();
				searchResults = searchResults.Where(songfile => {
					var songlabel = songfile.HumanLabel.ToLowerInvariant();
					var songlength = songfile.Length;
					List<int> songlengths;
					if (seen.TryGetValue(songlabel, out songlengths)) {
						bool tooSimilar = songlengths.Any(length => Math.Abs(songfile.Length - length) <= 5);
						if (!tooSimilar) {
							songlengths.Add(songlength);
							return true;
						}
						return false;
					} else {
						seen.Add(songlabel, new List<int> { songlength });
						return true;
					}
				});
			}
			if (topLimit.HasValue)
				searchResults = searchResults.Take(topLimit.Value);

			if (isXml) {

				var uriMapper = coreAttrsOnly ? SongDbContainer.LocalSongPathToAppRelativeMapper(context)
					: SongDbContainer.LocalSongPathToAbsolutePathMapper(context);
				new XDocument(
					viewXslt ? new XProcessingInstruction("xml-stylesheet", "type=\"text/xsl\"  href=\"searchresult.xsl?1\"") : null,
					new XElement("songs",
						new XAttribute("base", currentUrl),
						orderby.ToXml(),
						from s in searchResults
						select s.ConvertToXml(uri => uriMapper(uri).ToString(), coreAttrsOnly)
					)
				).Save(context.Response.Output);
			} else {
				var uriMapper = SongDbContainer.LocalSongPathToAbsolutePathMapper(context);
				foreach (ISongFileData songdata in searchResults)
					context.Response.Write(makeM3UEntry(songdata, extm3u, uriMapper));
			}
		}

		static string makeM3UEntry(ISongFileData song, bool extm3u, Func<Uri, Uri> makeAbsolute) {
			string url = makeAbsolute(song.SongUri).AbsoluteUri;//absoluteuri include escape sequences.
			if (extm3u)
				return "#EXTINF:" + song.Length + "," + song.HumanLabel + "\n" + url + "\n";
			else
				return url + "?" + HttpUtility.UrlEncode(song.HumanLabel) + "\n";
		}
	}
}