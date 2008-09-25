using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace LastFMspider.LfmApi
{
    public class LfmTag
    {
        public string name;
        [XmlElement]
        public int? count;

        [XmlIgnore]
        public bool countSpecified { get { return count.HasValue; } }
    }

    public class LfmTrackTopTags
    {
        [XmlAttribute("artist")]
        public string artist;

        [XmlAttribute("track")]
        public string trackTitle;

        [XmlElement("tag")]
        public LfmTag[] tag;
    }

    [XmlRoot("lfm")]
    public class ApiTrackGetTopTags
    {
        public LfmTrackTopTags toptags;
        [XmlAttribute] public string status;

        public static XmlSerializer MakeSerializer() { return new XmlSerializer(typeof(ApiTrackGetTopTags)); }
    }
}
