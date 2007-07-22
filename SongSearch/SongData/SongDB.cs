using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.XLinq;
using System.Xml;
using System.Query;
using System.IO;
using EamonExtensions;

namespace SongDataLib {
    public class SongDB {
        public SongData[] songs;

        public byte[] NormalizedSong(int si) {
            return norm(songs[si].FullInfo).ToArray();
        }
        public IEnumerable<byte[]> NormalizedSongs { get { return Sequence.Range(0, songs.Length).Select(si=>NormalizedSong(si)); } }
        public NormalizerDelegate norm;

        public SongDB(FileInfo file,NormalizerDelegate norm) {
            this.norm = norm;
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            XmlReader reader = XmlReader.Create(file.OpenText(),settings);
            List<SongData> songlist = new List<SongData>();
            while (reader.Read()) {
                if (!reader.IsEmptyElement)
                    continue;
                SongData song = new SongData();
                if(reader.Name != "song") throw new Exception("Data file format unknown, expectet xml document fragment consisting of 'song' elements");
                while(reader.MoveToNextAttribute()) {
                    switch(reader.Name) {
                        case "filepath": song.filepath = string.Intern(reader.Value); break;
                        case "title": song.title = string.Intern(reader.Value); break;
                        case "artist": song.artist= string.Intern(reader.Value); break;
                        case "performer": song.performer= string.Intern(reader.Value); break;
                        case "composer": song.composer = string.Intern(reader.Value); break;
                        case "album": song.album = string.Intern(reader.Value); break;
                        case "comment": song.comment= string.Intern(reader.Value); break;
                        case "genre": song.genre = string.Intern(reader.Value); break;
                        case "year": song.year = SongUtil.StringToNullableInt(reader.Value); break;
                        case "track": song.track = SongUtil.StringToNullableInt(reader.Value); break;
                        case "trackcount": song.trackcount = SongUtil.StringToNullableInt(reader.Value); break;
                        case "bitrate": song.bitrate = SongUtil.StringToNullableInt(reader.Value); break;
                        case "length": song.length = SongUtil.StringToNullableInt(reader.Value); break;
                        case "samplerate": song.samplerate = SongUtil.StringToNullableInt(reader.Value); break;
                        case "channels": song.channels = SongUtil.StringToNullableInt(reader.Value); break;
                    }
                }
                songlist.Add(song);
            }
            this.songs = songlist.ToArray();
            songlist = null;
        }
    }
}
