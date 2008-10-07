using System.Collections.Generic;
using System.Linq;
using SongDataLib;

namespace EamonExtensionsLinq.Algorithms.SuffixTreeInternals
{
	public class SuffixTreeNode : ISuffixTreeNode
	{
		internal Dictionary<byte, ISuffixTreeNode> children = new Dictionary<byte, ISuffixTreeNode>();
		internal List<Suffix> hits = new List<Suffix>();//in-order!
		internal int size;

		public int CompactAndCalcCost(SuffixTreeSongSearcher sssm) {
			hits.Capacity = hits.Count;
			size = hits.Count;
			foreach(byte next in children.Keys.ToArray()) {
				ISuffixTreeNode subTree = children[next];
				subTree = subTree.OptimalRep(sssm);
				children[next] = subTree;
				size += subTree.CompactAndCalcCost(sssm);
			}
			return size;
		}

		public void AddSuffix(SuffixTreeSongSearcher sssm, Suffix suffix) {
			byte next = sssm.GetNormedChar(suffix);
			if(next == SongUtil.TERMINATOR || next==SongUtil.MAXCANONBYTE) {
				hits.Add(suffix);
			} else {
				ISuffixTreeNode subTree;
				bool oldSubTree = children.TryGetValue(next, out subTree);
				if(!oldSubTree) children[next] = subTree = new SuffixTreeLeafNode();
				subTree.AddSuffix(sssm, suffix.Next);
				//				children[next] = subTree.OptimalRep(sssm);
			}
		}

		private IEnumerable<int> GetLocalSongs(SuffixTreeSongSearcher sssm) {
			return sssm.db.GetSongIndexes(hits);
		}


		public IEnumerable<int> GetAllSongs(SuffixTreeSongSearcher sssm) {
			return GetLocalSongs(sssm).Concat(
				Children.SelectMany(
					node => node.GetAllSongs(sssm))).Distinct();

			//	return SongUtil.RemoveDup(children.Values.Select(c => c.GetAllSongs(sssm)).Aggregate(this.GetLocalSongs(sssm), (a, b) => SongUtil.ZipUnion(a, b)));
		}


		public SearchResult Match(SuffixTreeSongSearcher sssm, int curdepth, byte[] query) {
			if(query.Length == curdepth) {
				return new SearchResult { cost = this.size, songIndexes = GetAllSongs(sssm) };//TODO improve using  ordering.
			} else
				return
						  children.ContainsKey(query[curdepth]) ?
						  children[query[curdepth]].Match(sssm, curdepth + 1, query) :
					 new SearchResult { cost = 0, songIndexes = Enumerable.Empty<int>() }
						  ;
		}


		public IEnumerable<ISuffixTreeNode> Desc {
			get {
				yield return this;
				foreach(ISuffixTreeNode child in Children)
					foreach(ISuffixTreeNode desc in child.Desc)
						yield return desc;
			}
		}

		public ISuffixTreeNode OptimalRep(SuffixTreeSongSearcher sssm) {
			return this;
		}
		public bool IsLeaf {
			get { return false; }
		}
		public IEnumerable<ISuffixTreeNode> Children { get { return children.Values; } }

		public IEnumerable<Suffix> LocalSuffixes {
			get { return hits; }
		}

	}
}
