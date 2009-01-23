using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using EmnExtensions.Text;
using System.Runtime.Serialization;

namespace LastFMspider
{
	[Serializable]
	public class SongSimilarityList
	{
		public SongRef songref;
		public SimilarTrack[] similartracks;
        public DateTime LookupTimestamp;
        public int? StatusCode;
        
	}
}
