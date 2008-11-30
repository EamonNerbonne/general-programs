using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using EmnExtensions;

namespace LastFMspider.OldApi
{
    
    public class TrackSimilarTracks
    {
        public class Artist
        {
            public string name;
        }
        public string name;
        public double match;
        public Artist artist;
    }

    [XmlRoot("similartracks")]
    public class ApiTrackSimilarTracks : XmlSerializableBase<ApiTrackSimilarTracks>
    {
        [XmlElement("track")]
        public TrackSimilarTracks[] track;
    }

}
