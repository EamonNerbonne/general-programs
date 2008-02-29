using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace LastFMspider
{
	[Serializable]
	public struct SimilarTrack
	{
		public double similarity;
		public SongRef similarsong;
	}
}
