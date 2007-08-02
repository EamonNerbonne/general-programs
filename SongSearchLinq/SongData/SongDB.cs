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
		public SongData[] songs;

		public string NormalizedSong(int si) {
			return Canonicalize.Basic(songs[si].FullInfo);
		}
		public IEnumerable<string> NormalizedSongs { get { return Enumerable.Range(0, songs.Length).Select(si => NormalizedSong(si)); } }


		public SongDB(IEnumerable<FileInfo> files) {
			XmlReaderSettings settings = new XmlReaderSettings();
			settings.ConformanceLevel = ConformanceLevel.Fragment;
			List<SongData> songlist = new List<SongData>();
			foreach(FileInfo file in files) {
				XmlReader reader = XmlReader.Create(file.OpenText(), settings);
				while(reader.Read()) {
					if(!reader.IsEmptyElement) continue;
					if(reader.Name != "song") throw new Exception("Data file format unknown, expected xml document fragment consisting of 'song' elements");
					songlist.Add(new SongData((XElement)XElement.ReadFrom(reader)));
					//if(songlist.Count > 10000) break;
				}
				//if(songlist.Count > 10000) break;
			}
			this.songs = songlist.ToArray();
			songlist = null;
		}
	}
}
