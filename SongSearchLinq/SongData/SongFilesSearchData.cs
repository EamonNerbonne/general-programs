using System;
using System.Collections.Generic;
using System.Linq;

namespace SongDataLib {
	public class SongFilesSearchData {
		public ISongFileData[] songs;
		readonly byte[] normed;//all normalized songs as one big string!
		public byte[] NormedSongs { get { return normed; } }
		readonly Suffix[] songBoundaries;//start positions of each song!
		public Suffix[] SongBoundaries { get { return songBoundaries; } }
		public int SongCount { get { return songs.Length; } }

		public IEnumerable<int> GetSongIndexes(List<Suffix> suffixList) {
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


		public int GetSongIndex(Suffix charpos) {
			return GetSongIndex(charpos, 0, SongCount);
		}

		public int GetSongIndex(Suffix charpos, int startI) {
			return GetSongIndex(charpos, startI, SongCount);
		}

		/// <summary>
		/// Searches efficiently for the song index belonging to a suffix, given that the songindex is in [startI,endI).
		/// </summary>
		/// <param name="suffix">The Suffix to search for</param>
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
			this.songs = songs.ToArray();
			//convert all songs into a single byte array...
			songBoundaries = new Suffix[SongCount + 1];
			int songCount = 0;
			songBoundaries[0] = (Suffix)0;
			List<byte> normedConstructor = new List<byte>();
			foreach (byte[] normSong in songs.Select(song => SongUtil.CanonicalizedSearchStr(song.FullInfo))) {
				normedConstructor.AddRange(normSong);
				normedConstructor.Add(SongUtil.TERMINATOR);
				songBoundaries[++songCount] = (Suffix)normedConstructor.Count;
			}
			normed = normedConstructor.ToArray();
			normedConstructor = null;
			//OK byte array 'normed' is constructed.
		}

		public byte[] NormalizedSong(int si) {
			int start = (int)SongBoundaries[si];
			int length = (int)SongBoundaries[si + 1] - start - 1;
			byte[] retval = new byte[length];
			Array.Copy(normed, start, retval, 0, length);
			return retval;
		}
	}
}
