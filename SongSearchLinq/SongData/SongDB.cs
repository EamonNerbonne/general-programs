using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using System.Linq;
using System.IO;
using EamonExtensionsLinq;
using EamonExtensionsLinq.Text;

namespace SongDataLib
{
	public class SongDB
	{
		public ISongData[] songs;

		public string NormalizedSong(int si) {
			return Canonicalize.Basic(songs[si].FullInfo);
		}
		public IEnumerable<string> NormalizedSongs { get { return Enumerable.Range(0, songs.Length).Select(si => NormalizedSong(si)); } }


		public SongDB(IEnumerable<ISongData> songs) {
			this.songs = songs.ToArray();
			/*var songlist =
				from file in files
				from songdata in SongDataFactory.LoadSongList(file)
				select songdata;
			this.songs = songlist.Where((sd,i)=>i%10==0).ToArray();*/
//			this.songs = songlist.ToArray();
		}
	}
}
