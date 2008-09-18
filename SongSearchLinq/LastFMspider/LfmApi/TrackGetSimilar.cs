using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace LastFMspider.LfmApi
{
    public class LfmArtist
    {
        public string name;
        public string mbid;
    }

    public class LfmSimilarTrack
    {
        public string name;
        public float match;
        public LfmArtist artist;
    }

    public class TrackGetSimilar
    {
        [XmlElement("track")]
        public LfmSimilarTrack[] track;
        
        [XmlAttribute("track")]  public string trackTitle;
        [XmlAttribute]   public string artist;
    }
    [XmlRoot("lfm")]
    public class LfmStatus
    {
        public TrackGetSimilar similartracks;
        [XmlAttribute]  public string status;
    }
}
