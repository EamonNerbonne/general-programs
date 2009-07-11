using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LastFMentityModel
{
	class TestClass
	{
		static void Test() {
			using (LastFMCacheModel model = new LastFMCacheModel()) {
				var q = from artist in model.Artist
						let ttl = artist.CurrentTopTracksList
						where ttl !=null 
						let overReach = ttl.TopTracks.Sum(tt=>tt.Reach)
						orderby overReach descending
						select artist;

				foreach (var artist in q.Take(100)) {
					Console.WriteLine(artist);
				}
			}
		}

		static void Main(string[] args) {
			Test();
		}
	}
}
