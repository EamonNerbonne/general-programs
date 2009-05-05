using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using EmnExtensions.Text;
using SongDataLib;
using SuffixTreeLib;
using HttpHeaderHelper;
using System.IO;
using System.Globalization;

namespace SongSearchSite
{
    public class PlaylistRequestProcessor : IHttpRequestProcessor
    {
        HttpRequestHelper helper;
        public PlaylistRequestProcessor(HttpRequestHelper helper) { this.helper = helper; }
        public void ProcessingStart() { }
        static DateTime startup = DateTime.Now;

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
                res.MimeType = "application/xml";
                isXml = true;
            }


            includeRemote = context.Request.QueryString["remote"] == "allow";
            extm3u = context.Request.QueryString["extm3u"] == "true";


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
			res.ETag = ResourceInfo.GenerateETagFrom(searchQuery, includeRemote, extm3u, isXml, extension, context.Request.QueryString["top"]);
            res.ResourceLength = null;//unknown
            res.TimeStamp = startup;
            Console.WriteLine("Request Determined: [" + (isXml ? 'X' : ' ') + (includeRemote ? 'R' : ' ') + (enc == Encoding.UTF8 ? 'U' : ' ') + (extm3u ? 'E' : ' ') + "] q=" + searchQuery);
            return res;
        }

        public DateTime? DetermineExpiryDate() {
            return DateTime.Now.AddDays(1);
        }

        public bool SupportRangeRequests {
            get { return false; }
        }

        public void WriteByteRange(Range range) {
            throw new NotImplementedException();
        }

        public void WriteEntireContent() {
            HttpContext context = helper.Context;

            if (extm3u && !isXml) context.Response.Write("#EXTM3U\n");
            string serverName = context.Request.Headers["Host"];
            string appName = context.Request.ApplicationPath;
            if (appName == "/") appName = "";
            string urlprefix = "http://" + serverName + appName + "/songs/";
            //			urlprefix = "http://home.nerbonne.org/";

            var searchResults = SongContainer.SearchableSongDB.Search(searchQuery);
            if (!includeRemote)
                searchResults = searchResults.Where(song => song.IsLocal);

            int onlyTop;
            if (int.TryParse(context.Request.QueryString["top"], out onlyTop)) {
                searchResults = searchResults.Take(onlyTop);
            }

            if (isXml) {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                XmlWriter xmlOut = XmlWriter.Create(context.Response.Output, settings);

                xmlOut.WriteStartDocument();
                if (context.Request.QueryString["view"] == "xslt") xmlOut.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\"  href=\"tableview.xsl\"");
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
            if (extm3u) return "#EXTINF:" + song.Length + "," + song.HumanLabel + "\n" + url + "\n";
            else return url + "?_=" + HttpUtility.UrlEncode(song.HumanLabel) + "\n";
        }

        static string makeUrl(string urlprefix, ISongData song) {
            if (song.IsLocal) { //http://www.albionresearch.com/misc/urlencode.php
                //http://www.blooberry.com/indexdot/html/topics/urlencoding.htm
                return UrlTranslator(urlprefix, song.SongPath);
            } else return song.SongPath;
        }


        static string UrlTranslator(string urlprefix, string songpath) {
            return urlprefix + System.Uri.EscapeDataString(SongContainer.NormalizeSongPath(songpath)).Replace("%2F", "/");
        }

        static Func<string, string> UrlTranslator(string urlprefix) {
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

    public class SongContainer
    {
        public static string NormalizeSongPath(string localSongPath) {
            StringBuilder sb = new StringBuilder();
            foreach (char c in localSongPath) {
                switch (c) {
                    case '\\':
                        sb.Append('/');
                        break;
                    case '%':
                    case '*':
                    case '&':
                    case ':':
                    case '!':
                        //we filter out %*&: to avoid triggering IIS7 "bad request bug" bug: http://support.microsoft.com/default.aspx?scid=kb;EN-US;826437
                        sb.Append('!');
                        sb.Append(Convert.ToString((int)c, 16).PadLeft(2, '0'));
                        break;
                    default:
                        if (Canonicalize.FastGetUnicodeCategory(c) == UnicodeCategory.Control)
                            goto case '!';//some filenames actually contain control chars and IIS chokes on em.
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        public static string NormalizeSongPath(ISongData localSong) {
            if (!localSong.IsLocal) throw new ArgumentException("This is only meaningful for local files.");
            return NormalizeSongPath(localSong.SongPath);
        }

        static SongContainer Singleton {
            get {
                HttpContext context = HttpContext.Current;
                lock (context.Application) {
                    SongContainer retval;
                    if (context.Application["SongContainer"] == null)
                        context.Application["SongContainer"] = retval = new SongContainer();
                    else retval = (SongContainer)context.Application["SongContainer"];
                    return retval;
                }
            }
        }
        SearchableSongDB searchEngine;
        SongDB db;

        Dictionary<string, ISongData> localSongs = new Dictionary<string, ISongData>();

        private SongContainer() {
            HttpContext context = HttpContext.Current;
            SongDatabaseConfigFile dcf = new SongDatabaseConfigFile(true);
            List<ISongData> tmpSongs = new List<ISongData>();
            dcf.Load(delegate(ISongData aSong, double ratio) {
                tmpSongs.Add(aSong);
            });
            db = new SongDB(tmpSongs // .Where((s,si)=>si==0||((int)Math.Sqrt(si-1) != (int)Math.Sqrt(si)) ) 
                );
            dcf = null;
            tmpSongs = null;
            foreach (ISongData song in db.songs.Where(s => s.IsLocal)) {
                string normalizedPath = NormalizeSongPath(song);
                localSongs.Add(normalizedPath, song);
            }
            searchEngine = new SearchableSongDB(db, new SuffixTreeSongSearcher());
        }
        public static SearchableSongDB SearchableSongDB { get { return Singleton.searchEngine; } }
        /// <summary>
        /// Determines whether a given path maps to an indexed, local song.  If it doesn't, it returns null.  If it does, it returns the meta data known about the song, including the song's "real" path.
        /// </summary>
        /// <param name="path">The normalized path </param>
        /// <returns></returns>
        public static ISongData GetSongByNormalizedPath(string path) {
            ISongData retval;
            if (!Singleton.localSongs.TryGetValue(path, out retval)) retval = null;
            return retval;
        }
    }
}