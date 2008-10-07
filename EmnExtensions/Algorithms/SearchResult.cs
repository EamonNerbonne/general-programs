using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EamonExtensionsLinq.Algorithms
{
    public struct SearchResult : IComparable<SearchResult>
    {
        public int cost;//This is a rough estimate which should be proportional to the number of songs this result will match.  The Matching might be lazy, so this is an optimization.
        public IEnumerable<int> songIndexes;
        public int CompareTo(SearchResult other) { return cost - other.cost; }
    }
}
