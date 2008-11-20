using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace LastFMspider.OldApi
{
    public class ArtistTopTracks
    {
        public string name;
        public string mbid;//always empty??
        public int reach;
        public string url;
    }

        [XmlRoot("similartists")]
    public class ApiArtistTopTracks
    {
            public string artist;
            public ArtistTopTracks[] track;
    }
}
