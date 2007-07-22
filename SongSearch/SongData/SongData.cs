using System;
using System.Collections.Generic;
using System.Text;
using System.Query;
using System.Xml.XLinq;
using System.Xml;
using TagLib;
using EamonExtensions;
using System.IO;
using System.Globalization;


namespace SongDataLib {
    public class SongData {
        public string filepath, title, artist, performer, composer, album, comment, genre;
        public int? year, track, trackcount, bitrate, length, samplerate, channels;

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
            bitrate = file.AudioProperties == null ? null : (int?)file.AudioProperties.Bitrate;
            length = file.AudioProperties == null ? null : (int?)file.AudioProperties.Length;
            samplerate = file.AudioProperties == null ? null : (int?)file.AudioProperties.SampleRate;
            channels = file.AudioProperties == null ? null : (int?)file.AudioProperties.Channels;
        }

        public SongData(XElement from) {
            filepath = string.Intern(from.Attribute("filepath").Value);
            title = string.Intern(from.Attribute("title").Value);
            artist = string.Intern(from.Attribute("artist").Value);
            performer = string.Intern(from.Attribute("performer").Value);
            composer = string.Intern(from.Attribute("composer").Value);
            album = string.Intern(from.Attribute("album").Value);
            comment = string.Intern(from.Attribute("comment").Value);
            genre = string.Intern(from.Attribute("genre").Value);
            year = SongUtil.StringToNullableInt(from.Attribute("year").Value);
            track = SongUtil.StringToNullableInt(from.Attribute("track").Value);
            trackcount = SongUtil.StringToNullableInt(from.Attribute("trackcount").Value);
            bitrate = SongUtil.StringToNullableInt(from.Attribute("bitrate").Value);
            length = SongUtil.StringToNullableInt(from.Attribute("length").Value);
            samplerate = SongUtil.StringToNullableInt(from.Attribute("samplerate").Value);
            channels = SongUtil.StringToNullableInt(from.Attribute("channels").Value);
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
                channels == null ? null :new XAttribute("channels", numtostring(channels))
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
