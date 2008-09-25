using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EamonExtensionsLinq.Web;
using System.Xml;
using System.IO;

namespace LastFMspider.LfmApi
{

    public class ApiClient
    {
        static readonly TimeSpan minReqDelta = new TimeSpan(0, 0, 0, 1);//no more than one request per second.
        static DateTime nextRequestWhenInternal = DateTime.Now;
        static object syncRoot = new object();

        static void waitUntilFree() {
            while(true) {
                TimeSpan sleepSpan;
                lock(syncRoot) {
                    DateTime nextRequestWhen = nextRequestWhenInternal;
                    var now = DateTime.Now;
                    if (nextRequestWhen > now) {
                        sleepSpan = nextRequestWhen - now;
                    } else {
                        nextRequestWhen = now + minReqDelta;//TODO: consider replacing now with the old time.
                        break;
                    }
                }
                System.Threading.Thread.Sleep(sleepSpan);
            }
        }


        const string APIkey = "47bd78458c61cfdf4fd1562c6b41e857";
        const string baseApiUrl = "http://ws.audioscrobbler.com/2.0/";

        static KeyValuePair<string,string> par(string paramName, string paramValue) {
            return new KeyValuePair<string, string>(paramName, paramValue);
        }
        static Uri MakeUri(string method, params KeyValuePair<string, string>[] otherParams) {
            StringBuilder sb = new StringBuilder();
            sb.Append(baseApiUrl);
            sb.Append("?");
            Action<string,string> addParam = (key,val) => {
                sb.Append(Uri.EscapeDataString(key));
                sb.Append("=");
                sb.Append(Uri.EscapeDataString(val));
                sb.Append("&");
            };
            addParam("method",method);
            addParam("api_key",APIkey);
            foreach(var kv in otherParams) 
                addParam(kv.Key,kv.Value);
            sb.Length = sb.Length-1;
            return new Uri(sb.ToString());
        }



        static public class Track
        {
            public static ApiTrackGetTopTags GetTopTags(string artist, string track) {
                //http://www.last.fm/api/show?service=289
                Uri reqUri = MakeUri("track.gettoptags",
                    par("artist",artist), par("track",track)
                    );
                var requestedData = UriRequest.Execute(reqUri);
                var xmlReader= XmlReader.Create( new StringReader(requestedData.ContentAsString));
                return (ApiTrackGetTopTags) ApiTrackGetTopTags.MakeSerializer().Deserialize(xmlReader);
            }
        }

        //todo:
        //http://www.last.fm/api/show?service=356 (getinfo)
        //http://www.last.fm/api/show?service=319 (getsimilar)
    }
}
