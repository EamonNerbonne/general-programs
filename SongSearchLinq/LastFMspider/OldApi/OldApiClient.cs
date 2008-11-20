using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.Web;
using System.Xml;

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

        public static Uri MakeUri(string category, string method, params string[] otherParams) {
            return new Uri(baseApiUrl + category + "/" + string.Join("", otherParams.Select(s => Uri.EscapeDataString(s) + "/").ToArray()) + method + ".xml");
        }

        public static UriRequest MakeUriRequest(string category, string method, params string[] otherParams) {
            waitUntilFree();
            return UriRequest.Execute(MakeUri(category, method, otherParams));
        }




    }
}

