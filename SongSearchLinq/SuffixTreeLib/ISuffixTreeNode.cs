using System.Collections.Generic;
using SongDataLib;

namespace EamonExtensionsLinq.Algorithms.SuffixTreeInternals
{
	public interface ISuffixTreeNode
	{
		int CompactAndCalcCost(SuffixTreeSongSearcher sssm);
		void AddSuffix(SuffixTreeSongSearcher sssm, Suffix suffix);
		IEnumerable<int> GetAllSongs(SuffixTreeSongSearcher sssm);
		IEnumerable<Suffix> LocalSuffixes { get; }
		SearchResult Match(SuffixTreeSongSearcher sssm, int curdepth, byte[] query);
		IEnumerable<ISuffixTreeNode> Desc { get; }
		IEnumerable<ISuffixTreeNode> Children { get; }
		ISuffixTreeNode OptimalRep(SuffixTreeSongSearcher sssm);
		bool IsLeaf { get; }
	}


}
