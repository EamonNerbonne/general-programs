
namespace SongDataLib {
	public struct Popularity {
		public int ArtistPopularity;
		public int TitlePopularity;
		public override string ToString() { return ArtistPopularity + "/" + TitlePopularity; }
	}
	public interface IPopularityEstimator {
		Popularity EstimatePopularity(string artist, string track);
	}
	public class NullPopularityEstimator : IPopularityEstimator {
		public Popularity EstimatePopularity(string artist, string track) {
			return new Popularity { ArtistPopularity = 0, TitlePopularity = 0 };
		}
	}
}
