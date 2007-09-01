using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using SongDataLib;

namespace SuffixTreeLib {

    public class SuffixTree {
        //Dictionary<byte, SuffixTree> children;
		 internal SuffixTree[] children;
       internal List<Suffix> hits = new List<Suffix>();
       internal int size;

        public Dictionary<int, int> CompactAndCalcSize() {
            hits.Capacity = hits.Count;
            Dictionary<int, int> distinct = new Dictionary<int,int>();
            foreach (Suffix suf in hits)
                distinct[suf.songIndex] = suf.songIndex;
            if (children != null) 
                foreach (SuffixTree tree in children)
						 if(tree!=null)
                    foreach(int si in tree.CompactAndCalcSize().Keys)
                        distinct[si] = si;
            size = distinct.Count;
            return distinct;
        }

        private void addtokid(SuffixTreeSongSearcher sssm,int curdepth, Suffix s) {
            byte[] buf = sssm.GetNormSong(s.songIndex);
            if(buf.Length == curdepth+s.startPos) {
                hits.Add(s); 
                return;
            }
            byte next = buf[curdepth+s.startPos];
            if (children[next]==null)//!children.ContainsKey(next))
                children[next] = new SuffixTree();
            children[next].AddSuffix(sssm,curdepth + 1, s);
        }

        public void AddSuffix(SuffixTreeSongSearcher sssm,int curdepth, Suffix s) {
            if (children == null) {
                hits.Add(s);
                if (hits.Count > 1000) {
						 children = new SuffixTree[SongUtil.MAXCANONBYTE+1]; //new Dictionary<byte, SuffixTree>();
                    List<Suffix> oldhits = hits;
                    hits = new List<Suffix>();
                    foreach (Suffix old in oldhits) 
                        addtokid(sssm, curdepth,old);
                }
            } else {
                addtokid(sssm,curdepth, s);
            }
        }

        private IEnumerable<int> AllSongsDup {
            get {
                return children == null ?
                        hits.Select(suf => suf.songIndex) :
                        hits.Select(suf => suf.songIndex).Concat(
                            from sub in children
									 where sub!=null
                            from s in sub.AllSongsDup
                            select s);
            }
        }

        private IEnumerable<int> FilterBy(SuffixTreeSongSearcher sssm,int curdepth, byte[] query) {
            foreach (Suffix suf in hits) {
                int i = curdepth;
                foreach (byte b in sssm.GetNormSong(suf.songIndex).Skip(suf.startPos + curdepth)) {
                    if (query[i] != b) {
                        break;
                    } else if (i == query.Length - 1) {
                        yield return suf.songIndex;
                        break;
                    } else {
                        i++;
                    }
                }
            }
        }

        public SearchResult Match(SuffixTreeSongSearcher sssm,int curdepth, byte[] query) {
            if (children == null) {
                if (query.Length == curdepth) {
                    return new SearchResult { cost = this.size, songIndexes = hits.Select(h=>h.songIndex).Distinct() };
                } else {
                    return new SearchResult { cost = this.size / 10, songIndexes = FilterBy(sssm,curdepth, query).Distinct() };
                }
            }
            if (query.Length == curdepth) {
                return new SearchResult { cost = this.size, songIndexes = AllSongsDup.Distinct() };
            } else
                return
                        children[query[curdepth]]!=null ?
                        children[query[curdepth]].Match(sssm,curdepth + 1, query) :
                    new SearchResult { cost = 0, songIndexes = Enumerable.Empty<int>() }
                        ;
        }

        public int RecursiveSize {
            get {
                return this.size + ((children == null) ? 0 : children.Where(c=>c!=null).Select(t => t.RecursiveSize).Sum());
            }
        }

		  internal IEnumerable<SuffixTree> Desc {
			  get {
				  yield return this;
				  if(children != null)
					  foreach(SuffixTree child in children)
						  if(child != null)
							  foreach(SuffixTree desc in child.Desc)
								  yield return desc;
			  }
		  }
    }
}
