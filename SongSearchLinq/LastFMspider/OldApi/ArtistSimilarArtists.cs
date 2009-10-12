using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using EmnExtensions;

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

    [XmlRoot("similarartists")]
    public class ApiArtistSimilarArtists: XmlSerializableBase<ApiArtistSimilarArtists>
    {
        [XmlAttribute("artist")]
        public string artistName;
        [XmlAttribute]
        public int streamable;
        [XmlAttribute]
        public string picture;
        [XmlAttribute]
        public string mbid;

        [XmlElement("artist")]
        public ArtistSimilarArtists[] artist;
    }
}
