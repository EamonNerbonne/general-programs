using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using SongDataLib;
using System.Collections;

namespace SuffixTreeLib
{
	public interface ISuffixTreeNode
	{
		int CompactAndCalcCost(SuffixTreeSongSearcher sssm);
		void AddSuffix(SuffixTemp suffixTemp);
		IEnumerable<int> GetAllSongs(SuffixTreeSongSearcher sssm);
		IEnumerable<int> GetAllSongsWhen(SuffixTreeSongSearcher sssm,BitArray filter,BitArray result);
		IEnumerable<Suffix> LocalSuffixes { get; }
		SearchResult Match(SuffixTreeSongSearcher sssm, int curdepth, byte[] query);
		SearchResult CompleteMatch(SuffixTreeSongSearcher sssm, int curdepth, byte[] query, BitArray andFilter, BitArray result);
		IEnumerable<ISuffixTreeNode> Desc { get; }
		IEnumerable<ISuffixTreeNode> Children { get; }
		ISuffixTreeNode OptimalRep(SuffixTreeSongSearcher sssm);
		bool IsLeaf { get; }
	}


	public class SuffixTreeLeafNode : ISuffixTreeNode
	{
		internal List<Suffix> hits = new List<Suffix>();//in-order!

		public int CompactAndCalcCost(SuffixTreeSongSearcher sssm) {
			hits.Capacity = hits.Count;
			return hits.Count;
		}

		public void AddSuffix(SuffixTemp suffixTemp){
			hits.Add(suffixTemp.key);
		}

		public IEnumerable<int> GetAllSongs(SuffixTreeSongSearcher sssm)  {
			return sssm.GetSongIndexes(hits);
		}
		public IEnumerable<int> GetAllSongsWhen(SuffixTreeSongSearcher sssm, BitArray filter,BitArray result) {
			foreach(int songIndex in sssm.GetSongIndexes(hits)) {
				if(!filter[songIndex]||result[songIndex]) continue;
				result[songIndex] = true;
				yield return songIndex;
			}
		}
		public SearchResult Match(SuffixTreeSongSearcher sssm, int curdepth, byte[] query) {
			if(query.Length == curdepth) {
				return new SearchResult { cost = hits.Count, songIndexes = GetAllSongs(sssm) };
			} else {//curdepth<query.Length
				return new SearchResult { cost = hits.Count / 5, songIndexes = FilterBy(sssm, curdepth, query).Distinct() };
			}
		}
		public SearchResult CompleteMatch(SuffixTreeSongSearcher sssm, int curdepth, byte[] query, BitArray andFilter, BitArray result) {
			if(query.Length == curdepth) 
				return new SearchResult { cost = hits.Count, songIndexes = GetAllSongsWhen(sssm,andFilter,result)};
			 else {//curdepth<query.Length
				return new SearchResult { cost = hits.Count/5, songIndexes =
					from suf in hits
					let songIndex = sssm.GetSongIndex(suf)
					where andFilter[songIndex] && !result[songIndex]
					let songStart =  sssm.GetSongBoundary(songIndex)
					let songBytes = sssm.GetNormSong(songIndex)
					let suffixRelStart = suf.AbsStartPos-songStart
					where songBytes.Length -suffixRelStart>= query.Length-curdepth //
					let querySuffixBytes = query.Skip(curdepth)
					let songSuffixBytes = songBytes.Skip((int)suffixRelStart).Take(query.Length-curdepth)
					where querySuffixBytes.SequenceEqual(songSuffixBytes)
					select (result[songIndex]=true)?songIndex:songIndex  //(delegate(int si){result[si]=true;return si;})(songIndex)
				};
			}
		}


		public IEnumerable<ISuffixTreeNode> Desc {
			get { yield return this; }
		}

		private IEnumerable<int> FilterBy(SuffixTreeSongSearcher sssm, int curdepth, byte[] query) {
			int songIndex = 0;
			foreach(Suffix suf in hits) {
				int i = curdepth;
				songIndex = sssm.GetSongIndex(suf, songIndex);
				uint songStart = sssm.GetSongBoundary(songIndex);
				foreach(byte b in sssm.GetNormSong(songIndex).Skip((int)(suf.AbsStartPos-songStart))) {
					if(query[i] != b) {
						break;
					} else if(i == query.Length - 1) {
						yield return songIndex;
						break;
					} else {
						i++;
					}
				}
			}
		}

		public ISuffixTreeNode OptimalRep(SuffixTreeSongSearcher sssm) {
			if(hits.Count < 100) {
				return this;
			} else {
				SuffixTreeNode newnode = new SuffixTreeNode();
				SuffixTemp suffixTemp = new SuffixTemp(sssm,hits[0]);
				newnode.AddSuffix(suffixTemp);
				for(int hi=1;hi < hits.Count;hi++) {
					suffixTemp = new SuffixTemp(sssm, hits[hi], suffixTemp.SongIndex);
					newnode.AddSuffix(suffixTemp);
				}
				return newnode;
			}
		}

		public bool IsLeaf {
			get { return true; }
		}

		public IEnumerable<ISuffixTreeNode> Children {
			get { return Enumerable.Empty<ISuffixTreeNode>(); }
		}

		public IEnumerable<Suffix> LocalSuffixes {//in-order!
			get { return hits; }
		}

	}

	public class SuffixTreeNode : ISuffixTreeNode
	{
		internal Dictionary<byte,ISuffixTreeNode> children= new Dictionary<byte,ISuffixTreeNode>();
		internal List<Suffix> hits = new List<Suffix>();//in-order!
		internal int size;

		public int CompactAndCalcCost(SuffixTreeSongSearcher sssm) {
			hits.Capacity = hits.Count;
			size = hits.Count + Children.Select(c => c.CompactAndCalcCost(sssm)).Sum();
			return size;
		}

		public void AddSuffix(SuffixTemp suffixTemp){
			if(suffixTemp.normed.Length == suffixTemp.depth) {
				hits.Add(suffixTemp.key);
			} else {
				byte next = suffixTemp.nextByte;
				ISuffixTreeNode subTree;
				bool oldSubTree = children.TryGetValue(next, out subTree);
				if(!oldSubTree) subTree = new SuffixTreeLeafNode();
				subTree.AddSuffix(suffixTemp.NextSuffix);
				children[next] = subTree.OptimalRep(suffixTemp.sssm);
			}
		}

		private IEnumerable<int> GetLocalSongs(SuffixTreeSongSearcher sssm) {
			return sssm.GetSongIndexes(hits);
		}


		public IEnumerable<int> GetAllSongs(SuffixTreeSongSearcher sssm) {
			return SongUtil.RemoveDup(children.Values.Select(c => c.GetAllSongs(sssm)).Aggregate(this.GetLocalSongs(sssm), (a, b) => SongUtil.ZipUnion(a, b)));
		}

		public IEnumerable<int> GetAllSongsWhen(SuffixTreeSongSearcher sssm,BitArray filter,BitArray result) {
			foreach(int songIndex in GetLocalSongs(sssm)) {
				if(result[songIndex]||!filter[songIndex]) continue;
				result[songIndex] = true;
				yield return songIndex;
			}
			foreach(ISuffixTreeNode child in Children) {
				foreach(int songIndex in child.GetAllSongsWhen(sssm, filter,result)) {
					yield return songIndex;
				}
			}
		}

		public SearchResult Match(SuffixTreeSongSearcher sssm, int curdepth, byte[] query) {
			if(query.Length == curdepth) {
				return new SearchResult { cost = this.size, songIndexes = GetAllSongs(sssm) };//TODO improve using ordering.
			} else
				return
						  children.ContainsKey(query[curdepth]) ?
						  children[query[curdepth]].Match(sssm, curdepth + 1, query) :
					 new SearchResult { cost = 0, songIndexes = Enumerable.Empty<int>() }
						  ;
		}

		public SearchResult CompleteMatch(SuffixTreeSongSearcher sssm, int curdepth, byte[] query, BitArray andFilter, BitArray result) {
			if(query.Length == curdepth)
				return new SearchResult { cost = this.size, songIndexes = GetAllSongsWhen(sssm, andFilter, result) };
			else if(children.ContainsKey(query[curdepth]))
				return children[query[curdepth]].CompleteMatch(sssm, curdepth + 1, query, andFilter, result);
			else
				return new SearchResult { cost = 0, songIndexes = Enumerable.Empty<int>() };
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
