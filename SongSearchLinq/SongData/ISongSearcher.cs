using System;
using System.Collections.Generic;
using System.Text;

namespace SongDataLib
{
    public delegate IEnumerable<byte> NormalizerDelegate(string str);

    public struct SearchResult : IComparable<SearchResult>
    {
        public int cost;
        public IEnumerable<int> songIndexes;
        public int CompareTo(SearchResult other) { return cost - other.cost; }
    }

    public interface ISongSearcher
    {
        void Init(SongDB db);
        SearchResult Query(byte[] query);
    }
}
