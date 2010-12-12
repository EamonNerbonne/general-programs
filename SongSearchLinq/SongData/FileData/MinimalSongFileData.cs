using System;
using System.Xml.Linq;

namespace SongDataLib {

	public class MinimalSongFileData : ISongFileData {
		protected readonly Uri songuri, baseUri;
		public virtual string FullInfo { get { return Uri.UnescapeDataString(songuri.Host + songuri.PathAndQuery); } }

		protected XAttribute makeUriAttribute(Func<Uri, string> urlTranslator) {
			return new XAttribute("songuri", !IsLocal || urlTranslator == null ? songuri.ToString() : urlTranslator(songuri));
		}

		public virtual XElement ConvertToXml(Func<Uri, string> urlTranslator, bool coreAttrsOnly) {
			return new XElement("songref", makeUriAttribute(urlTranslator));
		}

		public virtual int Length { get { return 0; } }

		public Uri SongUri { get { return songuri; } }

		public bool IsLocal { get { return songuri.IsFile; } }

		public virtual string HumanLabel { get { return System.IO.Path.GetFileNameWithoutExtension(songuri.LocalPath); } }

		protected static XAttribute MakeAttributeOrNull(XName attrname, object data) { return data == null ? null : new XAttribute(attrname, data); }
		protected static XAttribute MakeAttributeOrNull(XName attrname, int data) { return data == 0 ? null : new XAttribute(attrname, data); }
		protected static XAttribute MakeAttributeOrNull(XName attrname, double data) { return data == 0.0 ? null : new XAttribute(attrname, data); }

		public MinimalSongFileData(Uri baseUri, Uri songuri, bool? mustBeLocal) {
			if (songuri == null) throw new ArgumentNullException("songuri");
			if (!songuri.IsAbsoluteUri) throw new ArgumentOutOfRangeException("songuri", "uri must be absolute");
			if (baseUri!=null && !baseUri.IsAbsoluteUri) throw new ArgumentOutOfRangeException("baseUri", "uri must be absolute");
			if (mustBeLocal.HasValue && mustBeLocal != songuri.IsFile)
				throw new Exception("Supposedly " + (mustBeLocal.Value ? "" : "non-") + "local song isn't: " + songuri);
			this.songuri = songuri;
			this.baseUri = baseUri;
		}

		public MinimalSongFileData(Uri baseUri, XElement xEl, bool? isLocal) : this(baseUri, new Uri((string)xEl.Attribute("songuri"), UriKind.Absolute), isLocal) { }
	}

}
