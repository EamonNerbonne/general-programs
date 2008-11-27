using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using EmnExtensions;

namespace LastFMspider.OldApi
{
    public class ArtistTopTracks
    {
        public string name;
        public string mbid;//always empty??
        public int reach;
        public string url;
    }

    [XmlRoot("mostknowntracks")]
    public class ApiArtistTopTracks : XmlSerializableBase<ApiArtistTopTracks>
    {
            [XmlAttribute]
            public string artist;
            [XmlElement("track")]
            public ArtistTopTracks[] track;


    }
}
