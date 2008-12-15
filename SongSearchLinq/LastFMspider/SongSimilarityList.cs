using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using EmnExtensions.Text;
using System.Runtime.Serialization;

namespace LastFMspider
{
	[Serializable]
	public class SongSimilarityList
	{
		public SongRef songref;
		public SimilarTrack[] similartracks;
        public DateTime LookupTimestamp;
        
		public static SongSimilarityList CreateFromAudioscrobblerXml(SongRef songref, XDocument doc,DateTime downloaded) {
			var simtracksObj =
				from trackXml in doc.Elements("similartracks").Elements("track")
				select new SimilarTrack {
					similarity = (double)trackXml.Element("match"),
					similarsong = SongRef.Create( (string)trackXml.Element("artist").Element("name"), (string)trackXml.Element("name")),
                   
				};
			return new SongSimilarityList {
				songref = songref,
				similartracks = simtracksObj.ToArray(),
                 LookupTimestamp = downloaded
                 
			};
		}


	}
}
