using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Globalization;

namespace EmnExtensions.Web {
	public class UriRequest {
		public byte[] Content { get; private set; }
		public Uri Uri { get; private set; }
		public string EncodingName { get; private set; }
		public Encoding Encoding { get; private set; }
		public string ContentAsString { get { return Encoding.GetString(Content); } }


		const int BUFSIZE = 4096;

		UriRequest() { }
		static readonly Encoding FallbackEncoding = Encoding.UTF8;
		public static UriRequest Execute(Uri uri, CookieContainer cookies = null, Uri referer = null, string UserAgent = null, string PostData=null) {
			if (uri.Scheme.ToUpperInvariant() != "HTTP")
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Scheme \"{0}\" is unknown in Uri \"{1}\".", uri.Scheme, uri));

			var request = (HttpWebRequest)WebRequest.Create(uri);
			if (UserAgent != null)
				request.UserAgent = UserAgent;
			request.CookieContainer = cookies;
			request.Referer = referer == null ? null : referer.ToString();
			//request.AllowAutoRedirect = true;//this is the default
			request.Accept = "*/*";
			request.Headers["Accept-Language"] = "en-US,en;q=0.8";
			request.Headers["Accept-Charset"] = "utf-8,ISO-8859-1;q=0.7,*;q=0.3";
			request.Headers["Accept-Encoding"] = "gzip,deflate";
			//request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US)";
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			if (PostData != null) {
				request.Method = "POST";
				request.ContentType = "application/x-www-form-urlencoded";

				byte[] contentBytes = FallbackEncoding.GetBytes(PostData);
				request.ContentLength = contentBytes.Length;
				using (var reqStream = request.GetRequestStream())
					reqStream.Write(contentBytes, 0, contentBytes.Length);
			}

			UriRequest retval = new UriRequest { Uri = uri };

			using (var response = request.GetResponse()) {
				var httpResponse = (HttpWebResponse)response;

				string encodingName = httpResponse.ContentEncoding;
				if (string.IsNullOrEmpty(encodingName))
					encodingName = httpResponse.CharacterSet;
				if (string.IsNullOrEmpty(encodingName)) {
					retval.EncodingName = null;
					retval.Encoding = FallbackEncoding;
				} else {
					try {
						retval.Encoding = Encoding.GetEncoding(encodingName);
					} catch (ArgumentException) {
						retval.Encoding = FallbackEncoding;//fallback to UTF-8
						encodingName = null;
					}
					retval.EncodingName = encodingName;
				}


				var buf = new byte[BUFSIZE];
				using (var stream = httpResponse.GetResponseStream()) {
					int lastReadCount = stream.Read(buf, 0, BUFSIZE);
					using (MemoryStream returnBuf = new MemoryStream(lastReadCount)) {
						while (lastReadCount != 0) {
							returnBuf.Write(buf, 0, lastReadCount);
							lastReadCount = stream.Read(buf, 0, BUFSIZE);
						}
						retval.Content = returnBuf.ToArray();
					}
				}

				retval.StatusCode = httpResponse.StatusCode;
			}

			return retval;
		}

		public HttpStatusCode StatusCode { get; set; }
	}
}
