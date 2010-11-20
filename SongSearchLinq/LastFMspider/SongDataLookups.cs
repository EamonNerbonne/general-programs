using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;

namespace LastFMspider
{
	public class SongDataLookups
	{
		public readonly Dictionary<SongRef, List<SongFileData>> dataByRef = new Dictionary<SongRef, List<SongFileData>>();
		public readonly Dictionary<string, SongFileData> dataByPath = new Dictionary<string, SongFileData>();
		public int InvalidRefCount {get{return ignoreCount;}}
		public int SongRefCount {get{return dataByRef.Count;}}
		public int SondDataCount {get{return dataByPath.Count;}}

		bool AddSongLookup(SongFileData song,Func<SongRef,bool> filter) {
			dataByPath[song.SongUri.ToString()] = song;
			SongRef songref = SongRef.Create(song);
			if(songref == null || (filter!=null && !filter(songref))) return false;

			if(!dataByRef.ContainsKey(songref)) {
				dataByRef[songref] = new List<SongFileData>() { song };
			} else {
				dataByRef[songref].Add(song);
			}
			return true;
		}
		int ignoreCount;
		public SongDataLookups(IEnumerable<SongFileData> songs,Func<SongRef,bool> filter) {
			ignoreCount = 0;
			foreach(SongFileData song in songs)
				if(!AddSongLookup(song,filter))
					ignoreCount++;
			Console.WriteLine("Ignored {0} songs with incomplete tags", ignoreCount);
		}
	}
}
