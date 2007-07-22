using System;
using System.Collections.Generic;
using System.Text;
using System.Query;
using System.Xml.XLinq;
using System.Xml;
using System.Data.DLinq;
using System.Net;
using System.Net.Cache;
using System.IO;

namespace WebpageScraper {
    public class WebpageScrape {
        Uri uri;
        public WebpageScrape(Uri uri) { this.uri = uri; }
        public WebpageScrape(string uri) { this.uri = new Uri(uri); }

        string data;
        public string Data {
            get {
                if (data == null) {
                    WebRequest wr = HttpWebRequest.Create(uri);
                    wr.CachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable);
                    WebResponse ans= wr.GetResponse();
                    data = new StreamReader(ans.GetResponseStream(), Encoding.UTF8).ReadToEnd();
                }
                return data;
            }
        }

        public XDocument XDocument {
            get {
                return XDocument.Parse(Data.Substring(Data.IndexOf("<?xml")));
            }
        }

        public XmlDocument XmlDocument {
            get {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(Data);
                return doc;
            }
        }
    }
}
