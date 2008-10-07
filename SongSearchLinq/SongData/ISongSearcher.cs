using System;
using System.Collections.Generic;

namespace SongDataLib
{
	public delegate string NormalizerDelegate(string str);

	public struct SearchResult : IComparable<SearchResult>
	{
		public int cost;//This is a rough estimate which should be proportional to the number of songs this result will match.  The Matching might be lazy, so this is an optimization.
		public IEnumerable<int> songIndexes;
		public int CompareTo(SearchResult other) { return cost - other.cost; }
	}

	public interface ISongSearcher
	{
		void Init(SongDB db);
		SearchResult Query(byte[] query);
		//SearchResult CompleteQuery(string query, BitArray filter, BitArray result);
	}
}
