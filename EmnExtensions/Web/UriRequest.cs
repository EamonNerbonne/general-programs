using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace EmnExtensions.Web {
    public class UriRequest {
        public byte[] Content { get; private set; }
        public Uri Uri { get; private set; }
        public string EncodingName { get; private set; }
        public Encoding Encoding { get; private set; }
        public string ContentAsString { get { return Encoding.GetString(Content); } }


        const int BUFSIZE = 4096;

        private UriRequest() { }
        static readonly Encoding FallbackEncoding = Encoding.UTF8;
        public static UriRequest Execute(Uri uri) {
            if( uri.Scheme.ToLowerInvariant() != "http")
                throw new ArgumentException(string.Format("Scheme \"{0}\" is unknown in Uri \"{1}\".",uri.Scheme,uri));

            var request = (HttpWebRequest)WebRequest.Create(uri);

            UriRequest retval = new UriRequest { Uri = uri };
            
            using (var response = request.GetResponse()) {
                var httpReponse = (HttpWebResponse)response;
                
                try {
                    string encodingName = httpReponse.ContentEncoding;
                    if (encodingName == null || encodingName == "")
                        encodingName = httpReponse.CharacterSet;
                    if (encodingName == null || encodingName == "") {
                        retval.EncodingName = null;
                        retval.Encoding = FallbackEncoding;
                    } else {
                        retval.Encoding = Encoding.GetEncoding(encodingName);
                        retval.EncodingName = encodingName;
                    }
                        
                } catch {
                    retval.EncodingName = null;
                    retval.Encoding = FallbackEncoding;//fallback to UTF-8
                }

                var buf = new byte[BUFSIZE];
                using (var stream = httpReponse.GetResponseStream()) {
                    int lastReadCount = stream.Read(buf, 0, BUFSIZE);
                    using (MemoryStream returnBuf = new MemoryStream(lastReadCount)) {

                        while (lastReadCount != 0) {
                            returnBuf.Write(buf, 0, lastReadCount);
                            lastReadCount = stream.Read(buf, 0, BUFSIZE);
                        }
                        retval.Content = returnBuf.ToArray();
                    }
                }
            }

            return retval;

        }
    }
}
