using System.Collections.Generic;
using System.Linq;
using SongDataLib;
using EmnExtensions.Algorithms;

namespace SuffixTreeLib {
	public sealed class SuffixTreeNode : ISuffixTreeNode {
		IDictionary<byte, ISuffixTreeNode> children = new Dictionary<byte, ISuffixTreeNode>();
		IList<Suffix> hits = new List<Suffix>();//in-order!
		int size;

		public int CompactAndCalcCost(SuffixTreeSongSearcher sssm) {
			hits = hits.ToArray();
			size = hits.Count;
			children = new SortedDictionary<byte, ISuffixTreeNode>(children);
			foreach (var child in children.Values)
				size += child.CompactAndCalcCost(sssm);
			return size;
		}

		public ISuffixTreeNode AddSuffix(SuffixTreeSongSearcher sssm, Suffix suffix) {
			byte next = sssm.GetNormedChar(suffix);
			if (next == StringAsBytesCanonicalization.TERMINATOR || next == StringAsBytesCanonicalization.MAXCANONBYTE) {
				hits.Add(suffix);
			} else {
				ISuffixTreeNode subTree;
				bool oldSubTree = children.TryGetValue(next, out subTree);
				if (!oldSubTree) children[next] = subTree = new SuffixTreeLeafNode();
				ISuffixTreeNode newRep = subTree.AddSuffix(sssm, suffix.Next);
				if (subTree != newRep)
					children[next] = newRep;
			}
			return this;
		}

		private IEnumerable<int> GetLocalSongs(SuffixTreeSongSearcher sssm) {
			return sssm.db.GetSongIndexes(hits);
		}

		public IEnumerable<int> GetAllSongs(SuffixTreeSongSearcher sssm) {
			IEnumerable<int>[] all = new IEnumerable<int>[children.Count + 1];
			int idx = 0;
			foreach (var child in Children)
				all[idx++] = child.GetAllSongs(sssm);
			all[idx] = GetLocalSongs(sssm);
			return SortedUnionAlgorithm.SortedUnion(all);
		}

		public SearchResult Match(SuffixTreeSongSearcher sssm, int curdepth, byte[] query) {
			if (query.Length == curdepth) {
				return new SearchResult { cost = size, songIndexes = GetAllSongs(sssm) };
			} else
				return children.ContainsKey(query[curdepth])
					? children[query[curdepth]].Match(sssm, curdepth + 1, query)
					: new SearchResult { cost = 0, songIndexes = Enumerable.Empty<int>() };
		}

		public IEnumerable<ISuffixTreeNode> Desc {
			get {
				yield return this;
				foreach (ISuffixTreeNode child in Children)
					foreach (ISuffixTreeNode desc in child.Desc)
						yield return desc;
			}
		}

		IEnumerable<ISuffixTreeNode> Children { get { return children.Values; } }
	}
}
