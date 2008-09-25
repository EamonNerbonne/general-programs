using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace LastFMspider.LfmApi
{
    public class LfmTrackInfo
    {
        public int id;
        public string name;
        public int duration;
        public int listeners;
        public int playcount;
        public LfmArtistRef artist;

        public LfmTrackTopTags toptags;
    }

    [XmlRoot("lfm")]
    public class ApiTrackGetInfo
    {
        public LfmTrackInfo track;
        [XmlAttribute]
        public string status;

        public static XmlSerializer MakeSerializer() { return new XmlSerializer(typeof(ApiTrackGetSimilar)); }
    }
}
