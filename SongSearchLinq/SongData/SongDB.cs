using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using System.Linq;
using System.IO;
using EamonExtensionsLinq;

namespace SongDataLib {
    public class SongDB {
        public SongData[] songs;

        public byte[] NormalizedSong(int si) {
            return norm(songs[si].FullInfo).ToArray();
        }
        public IEnumerable<byte[]> NormalizedSongs { get { return Enumerable.Range(0, songs.Length).Select(si=>NormalizedSong(si)); } }
        public NormalizerDelegate norm;

        public SongDB(FileInfo file,NormalizerDelegate norm) {
            this.norm = norm;
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            XmlReader reader = XmlReader.Create(file.OpenText(),settings);
            List<SongData> songlist = new List<SongData>();
            XElement el;
            while (reader.Read()) {
                if (!reader.IsEmptyElement) continue;
                if(reader.Name != "song") throw new Exception("Data file format unknown, expected xml document fragment consisting of 'song' elements");
                song = new SongData(XElement.ReadFrom(reader));
                songlist.Add(song);
            }
            this.songs = songlist.ToArray();
            songlist = null;
        }
    }
}
