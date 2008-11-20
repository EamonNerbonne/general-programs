using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace LastFMspider.OldApi
{
    public class ArtistSimilarArtists
    {
        public string name;
        public string mbid;
        public float match;
        public string url;
        public string image_small;
        public string image;
        public int streamable;
    }

    [XmlRoot("similartists")]
    public class ApiArtistSimilarArtists
    {
        [XmlAttribute("artist")]
        public string artistName;
        [XmlAttribute]
        public int streamable;
        [XmlAttribute]
        public string picture;
        [XmlAttribute]
        public string mbid;

        public ArtistSimilarArtists[] artist;
    }
}
