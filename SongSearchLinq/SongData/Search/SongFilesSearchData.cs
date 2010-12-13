using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SongDataLib {
	public sealed class SongFilesSearchData {
		public readonly ISongFileData[] songs;
		public IEnumerable<SongFileData> Songs { get { return songs.OfType<SongFileData>(); } }
		readonly byte[] normed;//all normalized songs as one big string!
		public byte[] NormedSongs { get { return normed; } }
		readonly Suffix[] songBoundaries;//start positions of each song!
		public Suffix[] SongBoundaries { get { return songBoundaries; } }
		public int SongCount { get { return songs.Length; } }
		public readonly int SongFileCount;

		public IEnumerable<int> GetSongIndexes(IList<Suffix> suffixList) {
			if (suffixList.Count < 2) {
				if (suffixList.Count == 1) yield return GetSongIndex(suffixList[0]);
			} else {
				int sufI = 0;
				Suffix sufCurr = suffixList[sufI];
				int siCurr = GetSongIndex(sufCurr);
				int siLast = GetSongIndex(suffixList[suffixList.Count - 1], siCurr);
				Suffix sufLast = songBoundaries[siLast];
				yield return siCurr;

				sufI++;
				sufCurr = suffixList[sufI];
				while (sufCurr < sufLast) {
					int siNew = GetSongIndex(sufCurr, siCurr, siLast);
					if (siNew != siCurr) {
						yield return siNew;
						siCurr = siNew;
					}
					sufI++;
					sufCurr = suffixList[sufI];
				}
				if (siLast != siCurr) yield return siLast;
			}
		}
		internal IEnumerable<int> GetSongIndexesIter(IEnumerable<Suffix> increasingCharPos) {
			int songIndex = 0;
			Suffix minSufPos = (Suffix)0;
			foreach (Suffix suf in increasingCharPos) {
				if (suf < minSufPos) continue;
				songIndex = GetSongIndex(suf, songIndex);
				yield return songIndex;
				songIndex++;
				minSufPos = songBoundaries[songIndex];
			}
		}


		public int GetSongIndex(Suffix charpos) { return GetSongIndex(charpos, 0, SongCount); }

		public int GetSongIndex(Suffix charpos, int startI) { return GetSongIndex(charpos, startI, SongCount); }

		/// <summary>
		/// Searches efficiently for the song index belonging to a suffix, given that the songindex is in [startI,endI).
		/// </summary>
		/// <param name="target">The Suffix to search for</param>
		/// <param name="startI">Lower search bound, inclusive</param>
		/// <param name="endI">Upper search bound, exclusive</param>
		/// <returns>The index of the song.</returns>
		public int GetSongIndex(Suffix target, int startI, int endI) {

			Suffix start = songBoundaries[startI];
			Suffix end = songBoundaries[endI];

			if (target >= end || target < start) throw new IndexOutOfRangeException("Bounds invalid.");

			//calling songBoundaries f then:
			//INVARIANT:
			// start <= target < end
			// end == f( endI)
			//start == f(startI);

			while (startI + 1 != endI) {
				int valueRange = end - start;
				int indexRange = endI - startI - 1;//is at least  1
				int targetOffset = target - start;//targetOffset < valueRange,but positive

				//choose trialI such that: startI<trialI<endI
				int trialI = startI + 1 + (int)((ulong)targetOffset * (ulong)indexRange / (uint)valueRange);
				//rounds down, so startI+1<=trialI < startI +1 + indexRange ==EQUIV== startI<trialI<endI
				Suffix trial = songBoundaries[trialI];
				if (target < trial) { //trial is new end!
					end = trial;
					endI = trialI;
				} else { // trial <= target, so trial is new start!
					start = trial;
					startI = trialI;
				}
			}
			return startI;//f(startI) <= target < f(startI+1)
		}

		public SongFilesSearchData(IEnumerable<ISongFileData> songs) {
			//convert all songs into a single byte array...
			var songBoundariesList = new List<Suffix>(200000) { (Suffix)0 };
			var songList = new List<ISongFileData>(200000);
			using (var memStream = new MemoryStream()) {
				foreach (var song in songs) {
					songList.Add(song);
					if (song is SongFileData) SongFileCount++;
					var canonicalSongInfo = StringAsBytesCanonicalization.Canonicalize(song.FullInfo);
					memStream.Write(canonicalSongInfo, 0, canonicalSongInfo.Length);
					memStream.WriteByte(StringAsBytesCanonicalization.TERMINATOR);
					songBoundariesList.Add((Suffix)(int)memStream.Length);
				}
				normed = memStream.ToArray();
			}
			this.songs = songList.ToArray();
			songBoundaries = songBoundariesList.ToArray();
			//OK byte array 'normed' is constructed.
		}

		public static SongFilesSearchData FastLoad(SongDataConfigFile dcf, Func<ISongFileData, bool> filter = null) {
			BlockingCollection<ISongFileData> loadingSongs = new BlockingCollection<ISongFileData>();
			Task.Factory.StartNew(() => {
				dcf.Load((newsong, progress) => { if (filter == null || filter(newsong)) loadingSongs.Add(newsong); });
				loadingSongs.CompleteAdding();
			});
			return new SongFilesSearchData(loadingSongs.GetConsumingEnumerable());
		}

		public ByteRange NormalizedSong(int si) {
			int start = (int)SongBoundaries[si];
			int end = (int)SongBoundaries[si + 1] - 1;
			return new ByteRange(normed, start, end);
		}

		public struct SongIndexAndBytes {
			public readonly ByteRange bytes;
			public readonly int index;
			public SongIndexAndBytes(int index, ByteRange bytes) { this.bytes = bytes; this.index = index; }
		}


		public IEnumerable<SongIndexAndBytes> AllNormalizedSongs {
			get {
				int start = 0;
				int end = 0;
				for (int si = 0; si < SongCount; si++) {
					end = (int)SongBoundaries[si + 1];
					yield return new SongIndexAndBytes(si, new ByteRange(normed, start, end - 1));//exclude terminator
					start = end;
				}
			}
		}
	}
}
