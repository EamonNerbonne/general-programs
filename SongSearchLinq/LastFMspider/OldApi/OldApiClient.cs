using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.Web;
using System.Xml;
using System.IO;
using EmnExtensions;
using System.Net;

namespace LastFMspider.OldApi
{
    public class OldApiClient
    {
        static readonly TimeSpan minReqDelta = new TimeSpan(0, 0, 0, 1);//no more than one request per second.
        static DateTime nextRequestWhenInternal = DateTime.Now;
        static object syncRoot = new object();

        static void waitUntilFree() {
            while (true) {
                TimeSpan sleepSpan;
                DateTime nextRequestWhen;
                lock (syncRoot)
                    nextRequestWhen = nextRequestWhenInternal;
                var now = DateTime.Now;
                if (nextRequestWhen > now) {
                    sleepSpan = nextRequestWhen - now;
                } else {
                    lock (syncRoot)
                        nextRequestWhenInternal = nextRequestWhenInternal + minReqDelta;//todo: consider using DateTime.Now?
                    break;
                }

                System.Threading.Thread.Sleep(sleepSpan);
            }
        }


        const string baseApiUrl = "http://ws.audioscrobbler.com/1.0/";
        //TODO:double-escape data strings!!! LFM bug.
        public static Uri MakeUri(string category, string method, params string[] otherParams) {
            return new Uri(baseApiUrl + category + "/" + string.Join("",
                otherParams.Select(s => Uri.EscapeDataString(Uri.EscapeDataString(s).Replace(".", "%2e")) + "/").ToArray()) + method + ".xml");
        }

        public static UriRequest MakeUriRequest(string category, string method, params string[] otherParams) {
            waitUntilFree();
            return UriRequest.Execute(MakeUri(category, method, otherParams));
        }



        public static class Artist
        {
            public static ApiArtistTopTracks GetTopTracks(string artist) {
                try {
                    var req = MakeUriRequest("artist", "toptracks", artist);
                    var xmlReader = XmlReader.Create(new StringReader(req.ContentAsString ));
                    return XmlSerializableBase<ApiArtistTopTracks>.Deserialize(xmlReader);
                } catch (WebException we) { //if for some reason the server ain't happy...
                    HttpWebResponse wr = we.Response as HttpWebResponse;
                    if (wr.StatusCode == HttpStatusCode.NotFound)
                        return null;
                    else
                        throw;
                }
            }
            public static ApiArtistSimilarArtists GetSimilarArtists(string artist) {
                try {
                    var req = MakeUriRequest("artist", "similar", artist);
                    var xmlReader = XmlReader.Create(new StringReader(req.ContentAsString));
                    return XmlSerializableBase<ApiArtistSimilarArtists>.Deserialize(xmlReader);
                } catch (WebException we) { //if for some reason the server ain't happy...
                    HttpWebResponse wr = we.Response as HttpWebResponse;
                    if (wr.StatusCode == HttpStatusCode.NotFound)
                        return null;
                    else
                        throw;
                }

            }
        }
        public static class Track
        {
            public static ApiTrackSimilarTracks GetSimilarTracks(SongRef songref) {
                try {
                    var req = MakeUriRequest("track", "similar", songref.Artist,songref.Title);
                    var xmlReader = XmlReader.Create(new StringReader(req.ContentAsString));
                    return XmlSerializableBase<ApiTrackSimilarTracks>.Deserialize(xmlReader);
                } catch (WebException we) { //if for some reason the server ain't happy...
                    HttpWebResponse wr = we.Response as HttpWebResponse;
                    if (wr.StatusCode == HttpStatusCode.NotFound)
                        return null;
                    else
                        throw;
                }
            }
        }
    }
}

