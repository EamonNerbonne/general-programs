using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace LastFMspider.LfmApi
{

    public class LfmSimilarTrack
    {
        public string name;
        public float match;
        public LfmArtistRef artist;
    }

    public class LfmSimilarTracks
    {
        [XmlElement("track")]
        public LfmSimilarTrack[] track;
        
        [XmlAttribute("track")]  public string trackTitle;
        [XmlAttribute]   public string artist;
    }

    [XmlRoot("lfm")]
    public class ApiTrackGetSimilar
    {
        public LfmSimilarTracks similartracks;
        [XmlAttribute]  public string status;

        public static XmlSerializer MakeSerializer() { return new XmlSerializer(typeof(ApiTrackGetSimilar)); }
    }
}
