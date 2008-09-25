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
        public int count;
    }

    public class LfmTrackTopTags
    {
        [XmlAttribute("artist")]
        public string artist;

        [XmlAttribute("track")]
        public string trackTitle;

        public LfmTag[] tag;
    }

    [XmlRoot("lfm")]
    public class ApiTrackGetTopTags
    {
        public LfmTrackTopTags toptags;
        [XmlAttribute]
        public string status;

        public static XmlSerializer MakeSerializer() { return new XmlSerializer(typeof(ApiTrackGetSimilar)); }
    }
}
