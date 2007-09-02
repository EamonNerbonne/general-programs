using System;
using System.Collections.Generic;
using System.Text;
//using System.Query;
//using System.Xml.XLinq;
//using System.Xml;
using EamonExtensionsLinq.DebugTools;
using EamonExtensionsLinq.Filesystem;
using EamonExtensionsLinq;
using System.IO;
using System.Globalization;
using SongDataLib;
using System.Linq;
using System.Collections;

namespace SuffixTreeLib
{

	public struct Suffix
	{
		public uint AbsStartPos;
		public Suffix Next { get { return new Suffix { AbsStartPos = this.AbsStartPos + 1 }; } }
	}

	public struct SuffixTemp
	{
		public Suffix key;
		public byte[] normed;
		public int SongIndex;
		public SuffixTreeSongSearcher sssm;
		public int depth;
		public byte nextByte { get { return normed[depth]; } }
		public SuffixTemp NextSuffix {
			get {
				SuffixTemp nextSuf = this;
				nextSuf.key = key.Next;
				nextSuf.depth++;
				return nextSuf;
			}
		}

		public SuffixTemp(SuffixTreeSongSearcher sssm, int songIndex, Suffix suffix){
			this.sssm = sssm;
			this.key = suffix;
			SongIndex = songIndex;
			normed = sssm.GetNormSong(SongIndex);
			depth = (int)(key.AbsStartPos - sssm.GetSongBoundary(SongIndex));
		}
		public SuffixTemp(SuffixTreeSongSearcher sssm, Suffix suffix)
			: this(sssm, sssm.GetSongIndex(suffix), suffix) { }
		public SuffixTemp(SuffixTreeSongSearcher sssm, Suffix suffix, int startI)
			: this(sssm, sssm.GetSongIndex(suffix, startI), suffix) { }
		public SuffixTemp(SuffixTreeSongSearcher sssm, Suffix suffix, int startI, int endI) 
		: this(sssm, sssm.GetSongIndex(suffix, startI,endI), suffix) { }
	}

	public class SuffixTreeSongSearcher : ISongSearcher
	{
		ISuffixTreeNode tree;
		SongDB db;
		byte[][] normed;
		uint[] songBoundaries;
		int songCount;//updated during index building.
		public int SongCount { get { return songCount; } }
		public IEnumerable<int> GetSongIndexes(List<Suffix> suffixList) {
			if(suffixList.Count < 2) {
				if(suffixList.Count == 1) yield return GetSongIndex(suffixList[0]);
			} else {
				int sufI=0;
				Suffix sufCurr = suffixList[sufI];
				int siCurr = GetSongIndex(sufCurr);
				int siLast = GetSongIndex(suffixList[suffixList.Count - 1], siCurr);
				uint sufLast = GetSongBoundary(siLast);
				yield return siCurr;

				sufI++;
				sufCurr = suffixList[sufI];
				while(sufCurr.AbsStartPos <sufLast){
					int siNew = GetSongIndex(sufCurr, siCurr, siLast);
					if(siNew != siCurr) {
						yield return siNew;
						siCurr = siNew;
					}
					sufI++;
					sufCurr = suffixList[sufI];
				}
				if(siLast != siCurr) yield return siLast;
			}
		}


		public int GetSongIndex(Suffix suffix) {
			return GetSongIndex(suffix, 0, songCount);
		}

		public int GetSongIndex(Suffix suffix,int startI) {
			return GetSongIndex(suffix, startI, songCount);
		}

		/// <summary>
		/// Searches efficiently for the song index belonging to a suffix, given that the songindex is in [startI,endI).
		/// </summary>
		/// <param name="suffix">The Suffix to search for</param>
		/// <param name="startI">Lower search bound, inclusive</param>
		/// <param name="endI">Upper search bound, exclusive</param>
		/// <returns>The index of the song.</returns>
		public int GetSongIndex(Suffix suffix,int startI, int endI) {
			uint target = suffix.AbsStartPos;

			uint start = songBoundaries[startI];
			uint end = songBoundaries[endI];

			if(target >= end || target < start) throw new IndexOutOfRangeException("Bounds invalid.");

			//calling songBoundaries f then:
			//INVARIANT:
			// start <= target < end
			// end == f( endI)
			//start == f(startI);

			while(startI + 1 != endI) {
				uint valueRange = end - start;
				int indexRange = endI - startI - 1;//is at least  1
				uint targetOffset = target - start;//targetOffset < valueRange,but positive

				//choose trialI such that: startI<trialI<endI
				int trialI = startI+1+ (int)((ulong)targetOffset * (ulong)indexRange  /valueRange);
				//rounds down, so startI+1<=trialI < startI +1 + indexRange ==EQUIV== startI<trialI<endI
				uint trial = songBoundaries[trialI];
				if(target < trial) { //trial is new end!
					end = trial;
					endI = trialI;
				} else { // trial <= target, so trial is new start!
					start = trial;
					startI = trialI;
				}
			}
			return startI;//f(startI) <= target < f(startI+1)
		}

		public uint GetSongBoundary(int songIndex) {
			return songBoundaries[songIndex];
		}

		public SuffixTreeSongSearcher() { }
		public void Init(SongDB db) {
			this.db = db;
			tree = new SuffixTreeNode();
			normed = new byte[db.songs.Length][];
			songBoundaries = new uint[db.songs.Length+1];

			uint curpos = 0;
			songCount = 0;
			songBoundaries[0] = 0;
			for(int si = 0; si < db.songs.Length; si++) {
				
				
				uint songStart = curpos;

				byte[] buf = normed[si] = SongUtil.str2byteArr(db.NormalizedSong(si));
				curpos += (uint)buf.Length+1;//this many bytes accounted for, plus "boundary"
				songCount += 1;//this many songs accounted for
				songBoundaries[songCount] = curpos;//position of "first byte"
		
				for(int i = 0; i < buf.Length; i++)
					tree.AddSuffix(new SuffixTemp(this, si, new Suffix { AbsStartPos = songStart + (uint)i }));
			}
			/*long[] charcount = new long[256];
			foreach(byte[] norm in normed)
				foreach(byte letter in norm)
					charcount[letter]++;
			Console.WriteLine(string.Join("\n", charcount.Select((c, bt) => bt.ToString() + " \'" + ((char)bt) + "\' " + c).ToArray()));*/
			//normed = null;
			//tree.CompactAndCalcSize(this).Count();
			/*
			var treeNodes = tree.Desc.ToArray();
			var leaves = treeNodes.Where(n=>n.IsLeaf).ToArray();
			var nonleaves = treeNodes.Where(n=>!n.IsLeaf).ToArray();
			Console.WriteLine("Number: " + treeNodes.Length);
			Console.WriteLine("Of which leaves: " + leaves.Length);
			Console.WriteLine("Of which nonleaves: " + nonleaves.Length);
			Console.WriteLine("Av Child#NonLeaf: " + nonleaves.Select(n => n.Children.Count()).Average());
			Console.WriteLine("Av Suf#Leaf: " + leaves.Select(n => n.LocalSuffixes.Count()).Average());
			Console.WriteLine("Av Suf#NonLeaf: " + nonleaves.Select(n => n.LocalSuffixes.Count()).Average());
			Console.WriteLine("Av Song#Leaf: " + leaves.Select(n => n.LocalSuffixes.Select(suf => GetSongIndex(suf)).Distinct().Count()).Average());
			Console.WriteLine("Av Song#NonLeaf: " + nonleaves.Select(n => n.LocalSuffixes.Select(suf => GetSongIndex(suf)).Distinct().Count()).Average());
			//*/


		}

		public SearchResult Query(string strquery) {
			byte[] query = SongUtil.str2byteArr(strquery);
			return tree.Match(this, 0, query);
		}

		public SearchResult CompleteQuery(string strquery, BitArray filter, BitArray result) {
			byte[] query = SongUtil.str2byteArr(strquery);
			return tree.CompleteMatch(this, 0, query,filter,result);
		}

		public byte[] GetNormSong(Suffix suf) {
			int songIndex = GetSongIndex(suf);
			return GetNormSong(songIndex);
		}

		public byte[] GetNormSong(int songIndex) {
			if(normed != null)
				return normed[songIndex];
			else
				return SongUtil.str2byteArr(db.NormalizedSong(songIndex));
		}
	}
}


