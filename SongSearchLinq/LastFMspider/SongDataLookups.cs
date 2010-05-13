using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;

namespace LastFMspider
{
	public class SongDataLookups
	{
		public readonly Dictionary<SongRef, List<SongData>> dataByRef = new Dictionary<SongRef, List<SongData>>();
		public readonly Dictionary<string, SongData> dataByPath = new Dictionary<string, SongData>();
		public int InvalidRefCount {get{return ignoreCount;}}
		public int SongRefCount {get{return dataByRef.Count;}}
		public int SondDataCount {get{return dataByPath.Count;}}

		bool AddSongLookup(SongData song,Func<SongRef,bool> filter) {
			dataByPath[song.SongUri.ToString()] = song;
			SongRef songref = SongRef.Create(song);
			if(songref == null || (filter!=null && !filter(songref))) return false;

			if(!dataByRef.ContainsKey(songref)) {
				dataByRef[songref] = new List<SongData>() { song };
			} else {
				dataByRef[songref].Add(song);
			}
			return true;
		}
		int ignoreCount;
		public SongDataLookups(IEnumerable<SongData> songs,Func<SongRef,bool> filter) {
			ignoreCount = 0;
			foreach(SongData song in songs)
				if(!AddSongLookup(song,filter))
					ignoreCount++;
			Console.WriteLine("Ignored {0} songs with incomplete tags", ignoreCount);
		}
	}
}
