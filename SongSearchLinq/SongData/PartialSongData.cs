using System;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using EamonExtensionsLinq;
using EamonExtensionsLinq.Text;
using System.Web;

namespace SongDataLib
{
	public class PartialSongData : MinimalSongData
	{
		public string label;
		public int length;

		public override string FullInfo {
			get {
				if(label == null) return SongPath;
				else return HttpUtility.UrlDecode( SongPath) + "\n" + label;
			}
		}

		public override XElement ConvertToXml(Func<string, string> urlTranslator) {
			return new XElement("partsong",
				makeUriAttribute(urlTranslator),
				label == null ? null : new XAttribute("label", label),
				length == 0 ? null : new XAttribute("length", length.ToString())
				);
		}
		public override int Length { get { return length; } }
		public override string HumanLabel { get { return label ?? base.HumanLabel; } }

		static readonly Regex extm3uPattern = new Regex(@"^#EXTINF:(?<songlength>[0-9]+),(?<label>.*)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		internal PartialSongData(XElement xEl, bool? isLocal)
			: base(xEl, isLocal) {
			label = (string)xEl.Attribute("label");//might even be null!
			length = ParseString.ParseAsInt32((string)xEl.Attribute("length")) ??0;//might even be null!
		}

		internal PartialSongData(string extm3ustr, string url, bool? isLocal)
			: base(url, isLocal) {
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
