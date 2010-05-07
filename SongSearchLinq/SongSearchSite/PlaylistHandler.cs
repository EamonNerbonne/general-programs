using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using EmnExtensions.Text;
using HttpHeaderHelper;
using SongDataLib;

namespace SongSearchSite
{
	public class PlaylistRequestProcessor : IHttpRequestProcessor
	{
		HttpRequestHelper helper;
		public PlaylistRequestProcessor(HttpRequestHelper helper) { this.helper = helper; }
		public void ProcessingStart() { }
		static DateTime startupUtc = DateTime.UtcNow;

		bool isXml, includeRemote, extm3u;
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
				context.Response.Headers["Content-Disposition"] = "attachment; filename=" + Uri.EscapeDataString( "playlist_" + searchQuery + extension); //searchquery has been canonicalized: no dangerous injection possible.

			res.ETag = ResourceInfo.GenerateETagFrom(searchQuery, includeRemote, extm3u, isXml, extension, context.Request.QueryString["top"]);
			res.ResourceLength = null;//unknown
			res.TimeStamp = startupUtc;
			Console.WriteLine("Request Determined: [" + (isXml ? 'X' : ' ') + (includeRemote ? 'R' : ' ') + (enc == Encoding.UTF8 ? 'U' : ' ') + (extm3u ? 'E' : ' ') + "] q=" + searchQuery);
			return res;
		}

		public DateTime? DetermineExpiryDate() {
			return DateTime.UtcNow.AddHours(1);
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
			string urlprefix = "http://" + serverName + appName + "/songs/";
			//			urlprefix = "http://home.nerbonne.org/";

			var searchResults = SongDbContainer.SearchableSongDB.Search(searchQuery);
			if (!includeRemote)
				searchResults = searchResults.Where(song => song.IsLocal);

			int onlyTop;
			if (int.TryParse(context.Request.QueryString["top"], out onlyTop)) {
				searchResults = searchResults.Take(onlyTop);
			}

			if (isXml) {
				XmlWriterSettings settings = new XmlWriterSettings();
				//settings.Indent = true;
				XmlWriter xmlOut = XmlWriter.Create(context.Response.Output, settings);

				xmlOut.WriteStartDocument();
				if (context.Request.QueryString["view"] == "xslt")
					xmlOut.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\"  href=\"searchresult.xsl\"");
				xmlOut.WriteStartElement("songs");
				foreach (ISongData s in searchResults) {
					s.ConvertToXml(UrlTranslator(urlprefix)).WriteTo(xmlOut);
				}
				xmlOut.WriteEndElement();
				xmlOut.WriteEndDocument();

				xmlOut.Close();
			} else {
				foreach (ISongData songdata in searchResults) {
					context.Response.Write(makeM3UEntry(songdata, extm3u, urlprefix));
				}
			}
		}
		static string makeM3UEntry(ISongData song, bool extm3u, string urlprefix) {
			string url = makeUrl(urlprefix, song);
			if (extm3u)
				return "#EXTINF:" + song.Length + "," + song.HumanLabel + "\n" + url + "\n";
			else
				return url + "?_=" + HttpUtility.UrlEncode(song.HumanLabel) + "\n";
		}

		static string makeUrl(string urlprefix, ISongData song) {
			if (song.IsLocal) { //http://www.albionresearch.com/misc/urlencode.php
				//http://www.blooberry.com/indexdot/html/topics/urlencoding.htm
				return UrlTranslator(urlprefix, song.SongUri).ToString();
			} else
				return song.SongUri.ToString();
		}


		static string UrlTranslator(string urlprefix, Uri songpath) {
			return urlprefix + System.Uri.EscapeDataString(SongDbContainer.NormalizeSongPath(songpath)).Replace("%2F", "/");
		}

		static Func<Uri, string> UrlTranslator(string urlprefix) {
			return s => UrlTranslator(urlprefix, s);
		}
	}

	public class PlaylistHandler : IHttpHandler
	{

		public bool IsReusable { get { return false; } }

		public void ProcessRequest(HttpContext context) {
			Console.WriteLine("PlaylistHandler called.");
			HttpRequestHelper helper = new HttpRequestHelper(context);
			PlaylistRequestProcessor proc = new PlaylistRequestProcessor(helper);
			helper.Process(proc);
		}
	}
}