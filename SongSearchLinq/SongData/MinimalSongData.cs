using System;
using System.Web;
using System.Xml.Linq;
using EmnExtensions.Filesystem;
using System.Text;

namespace SongDataLib {

	public class MinimalSongData : ISongData {
		protected Uri songuri;
		protected bool isLocal;
		public virtual string FullInfo { get { return Uri.UnescapeDataString(songuri.Host + songuri.PathAndQuery); } }

		protected XAttribute makeUriAttribute(Func<Uri, string> urlTranslator) {
			if (!IsLocal || urlTranslator == null) {
				return new XAttribute("songuri", songuri);
			}
			else {
				return new XAttribute("songuri", urlTranslator(songuri));
			}
		}

		public virtual XElement ConvertToXml(Func<Uri, string> urlTranslator) {
			return new XElement("songref", makeUriAttribute(urlTranslator));
		}

		public virtual int Length { get { return 0; } }

		public virtual Uri SongUri { get { return songuri; } }

		public virtual bool IsLocal { get { return isLocal; } }

		public virtual string HumanLabel {
			get {
				return FullInfo;
			}
		}

		protected static XAttribute MakeAttributeOrNull(XName attrname, object data) {	return data == null ? null : new XAttribute(attrname, data);}
		protected static XAttribute MakeAttributeOrNull(XName attrname, int data) { return data == 0 ? null : new XAttribute(attrname, data); }

		public MinimalSongData(Uri songuri, bool? isLocal) {
			if (songuri == null) throw new ArgumentNullException("songuri");
			if (!songuri.IsAbsoluteUri) throw new ArgumentOutOfRangeException("songuri", "uri must be absolute");
			this.isLocal = songuri.IsFile;
			this.songuri = songuri;
			if (isLocal.HasValue && isLocal != this.isLocal)
				throw new Exception("Supposedly " + (isLocal.Value ? "" : "non-") + "local song isn't: " + songuri);
		}

		static Uri loadUri(XElement from, bool isLocal) {
			string tmp = (string)from.Attribute("uriUtfB64");//preferred place to put base64 data
			if (tmp != null)
				return new Uri(Encoding.UTF8.GetString(Convert.FromBase64String(tmp)), UriKind.Absolute);
			return new Uri((string)from.Attribute("songuri"), UriKind.Absolute);//old versions stuck base64 data in songuri
		}

		public MinimalSongData(XElement xEl, bool? isLocal) : this(loadUri(xEl, isLocal == true), isLocal) { }
	}

}
