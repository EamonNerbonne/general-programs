using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using TagLib;
using EamonExtensionsLinq;
using System.IO;
using System.Globalization;


namespace SongDataLib {
    public class SongData {
        public string filepath, title, artist, performer, composer, album, comment, genre;
        public int? year, track, trackcount, bitrate, length, samplerate, channels;
        public DateTime? lastWriteTime;

        public string FullInfo {
            get {
                StringBuilder sb = new StringBuilder();
                sb.Append(nonnullstring(title));
                sb.Append('\t');
                sb.Append(nonnullstring(artist));
                sb.Append('\t');
                sb.Append(nonnullstring(filepath));
                sb.Append('\t');
                sb.Append(nonnullstring(performer));
                sb.Append('\t');
                sb.Append(nonnullstring(composer));
                sb.Append('\t');
                sb.Append(nonnullstring(album));
                sb.Append('\t');
                sb.Append(nonnullstring(comment));
                sb.Append('\t');
                sb.Append(nonnullstring(genre));
                sb.Append('\t');
                sb.Append(nonnullstring(year));
                sb.Append('\t');
                sb.Append(nonnullstring(track));
                sb.Append('\t');
                sb.Append(nonnullstring(trackcount));
                sb.Append('\t');
                sb.Append(nonnullstring(bitrate));
                sb.Append('\t');
                sb.Append(nonnullstring(length));
                sb.Append('\t');
                sb.Append(nonnullstring(samplerate));
                sb.Append('\t');
                sb.Append(nonnullstring(channels));
                return sb.ToString();
            }
        }



        static string makesafe(string data) { return data == null ? data : new string(data.Select(c=> c == '\n' || c == '\t' ? ' ' : c).Where(c => c >= ' ').ToArray()); }
        static string makesafe(string[] data) { return data == null ? null : makesafe(string.Join(", ", data)); }

        public SongData(FileInfo fileObj) {
            TagLib.File file = TagLib.File.Create(fileObj.FullName);
            filepath = makesafe(file.Name);
            title = makesafe(file.Tag.Title);
            artist = makesafe(file.Tag.Artists);
            performer = makesafe(file.Tag.Performers);
            composer = makesafe(file.Tag.Composers);
            album = makesafe(file.Tag.Album);
            comment = makesafe(file.Tag.Comment);
            genre = makesafe(file.Tag.Genres);
            year = (int?)file.Tag.Year;
            track = (int?)file.Tag.Track;
            trackcount = (int?)file.Tag.TrackCount;
            lastWriteTime = fileObj.LastWriteTime;
            bitrate = file.AudioProperties == null ? null : (int?)file.AudioProperties.Bitrate;
            length = file.AudioProperties == null ? null : (int?)file.AudioProperties.Length;
            samplerate = file.AudioProperties == null ? null : (int?)file.AudioProperties.SampleRate;
            channels = file.AudioProperties == null ? null : (int?)file.AudioProperties.Channels;
        }

        public SongData(XElement from)
        {
            filepath = (string)from.Attribute("filepath");
            title = (string)from.Attribute("title");
            artist = (string)from.Attribute("artist");
            performer = (string)from.Attribute("performer");
            composer = (string)from.Attribute("composer");
            album = (string)from.Attribute("album");
            comment = (string)from.Attribute("comment");
            genre = (string)from.Attribute("genre");
            year = SongUtil.StringToNullableInt((string)from.Attribute("year"));
            track = SongUtil.StringToNullableInt((string)from.Attribute("track"));
            trackcount = SongUtil.StringToNullableInt((string)from.Attribute("trackcount"));
            bitrate = SongUtil.StringToNullableInt((string)from.Attribute("bitrate"));
            length = SongUtil.StringToNullableInt((string)from.Attribute("length"));
            samplerate = SongUtil.StringToNullableInt((string)from.Attribute("samplerate"));
            channels = SongUtil.StringToNullableInt((string)from.Attribute("channels"));
        }

        public SongData() { }

        private static string nonnullstring(string input) { return input ?? ""; }
        private static string nonnullstring(int? input) { return input==null?"":input.ToString(); }

        private static string numtostring(int? num) {
            return num == null ? "null" : num.ToString();
        }

        public XElement ConvertToXml() {
            return new XElement("song",
                filepath==null?null:new XAttribute("filepath", filepath),
                title == null ? null :new XAttribute("title", title),
                artist == null ? null :new XAttribute("artist", artist),
                performer == null ? null :new XAttribute("performer", performer),
                composer == null ? null :new XAttribute("composer", composer),
                album == null ? null :new XAttribute("album", album),
                comment == null ? null :new XAttribute("comment", comment),
                genre == null ? null :new XAttribute("genre", genre),
                year == null ? null :new XAttribute("year", numtostring(year)),
                track == null ? null :new XAttribute("track", numtostring(track)),
                trackcount == null ? null :new XAttribute("trackcount", numtostring(trackcount)),
                bitrate == null ? null :new XAttribute("bitrate", numtostring(bitrate)),
                length == null ? null :new XAttribute("length", numtostring(length)),
                samplerate == null ? null :new XAttribute("samplerate", numtostring(samplerate)),
                channels == null ? null :new XAttribute("channels", numtostring(channels)),
                lastWriteTime==null?null:new XAttribute("lastmodified",lastWriteTime.Value.ToString())
            );
        }

        public IEnumerable<string> Values {//only yield "distinctive search values
            get {
                yield return filepath;
                yield return title;
                yield return artist;
                yield return performer;
                yield return composer;
                yield return album;
                yield return comment;
                yield return genre;
                yield return numtostring(year);
                yield return numtostring(track);
            }
        }

    }
}
