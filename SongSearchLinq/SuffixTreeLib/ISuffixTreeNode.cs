using System.Collections.Generic;
using SongDataLib;

namespace SuffixTreeLib
{
	public interface ISuffixTreeNode
	{
		int CompactAndCalcCost(SuffixTreeSongSearcher sssm);
		ISuffixTreeNode AddSuffix(SuffixTreeSongSearcher sssm, Suffix suffix);
		IEnumerable<int> GetAllSongs(SuffixTreeSongSearcher sssm);
		SearchResult Match(SuffixTreeSongSearcher sssm, int curdepth, byte[] query);
		IEnumerable<ISuffixTreeNode> Desc { get; }
	}
}
