using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using EamonExtensionsLinq.Text;
using System.Runtime.Serialization;

namespace LastFMspider
{
	[Serializable]
	public class SongSimilarityList
	{
		public SongRef songref;
		public SimilarTrack[] similartracks;

		public static SongSimilarityList CreateFromAudioscrobblerXml(SongRef songref, XDocument doc) {
			var simtracksObj =
				from trackXml in doc.Elements("similartracks").Elements("track")
				select new SimilarTrack {
					similarity = (double)trackXml.Element("match"),
					similarsong = SongRef.Create( (string)trackXml.Element("artist").Element("name"), (string)trackXml.Element("name"))
				};
			return new SongSimilarityList {
				songref = songref,
				similartracks = simtracksObj.ToArray()
			};
		}

		public static SongSimilarityList CreateFromXElement(XElement xEl) {
			SongSimilarityList retval = new SongSimilarityList();
			retval.songref = SongRef.CreateFromXml(xEl);
			retval.similartracks =
				(from simEl in xEl.Elements("similarTo")
				 select new SimilarTrack {
					 similarsong = SongRef.CreateFromXml(simEl),
					 similarity = (double)simEl.Attribute("match")
				 }).ToArray();
			return retval;
		}

		public XElement ToXElement() {
			return new XElement("similarsongs",
				songref.ToXml(),
				from similartrack in similartracks
				select new XElement("similarTo",
					similartrack.similarsong.ToXml(),
					new XAttribute("match", similartrack.similarity)
				));
		}

	}
}
