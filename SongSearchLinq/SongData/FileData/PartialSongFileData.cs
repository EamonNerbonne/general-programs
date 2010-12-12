using System;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using EmnExtensions;
using EmnExtensions.Text;
using System.Web;

namespace SongDataLib
{
	public class PartialSongFileData : MinimalSongFileData
	{
		public string label;
		public int length;

		public override string FullInfo {
			get {
				if(label == null) return base.FullInfo;
				else return base.FullInfo + "\n" + label;
			}
		}

		public override XElement ConvertToXml(Func<Uri, string> urlTranslator,bool coreAttrsOnly) {
			return new XElement("partsong",
				makeUriAttribute(urlTranslator),
				MakeAttributeOrNull("label", label),
				MakeAttributeOrNull("length", length)
				);
		}
		public override int Length { get { return length; } }
		public override string HumanLabel { get { return label ?? base.HumanLabel; } }

		static readonly Regex extm3uPattern = new Regex(@"^#EXTINF:(?<songlength>[0-9]+),(?<label>.*)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		internal PartialSongFileData(Uri baseUri, XElement xEl, bool? isLocal)
			: base(baseUri,xEl, isLocal) {
			label = (string)xEl.Attribute("label");//might even be null!
			length = ((int?)xEl.Attribute("length")) ??0;//might even be null!
		}

		internal PartialSongFileData(Uri baseUri, string extm3ustr, Uri url, bool? isLocal)
			: base(baseUri, url, isLocal) {
			Match m;
			lock(extm3uPattern) m = extm3uPattern.Match(extm3ustr);
			if(m.Success) {
				length = m.Groups["songlength"].Value.ParseAsInt32() ??0;
				label = m.Groups["label"].Value;
				if(label == "") label = null;
			} else {
				length = 0;
				label = null;
				throw new Exception("PartialSongData being constructed from non-EXTM3U string, impossible");
			}
		}
	}

}
