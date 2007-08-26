using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using TagLib;
using EamonExtensionsLinq;
using EamonExtensionsLinq.Text;
using EamonExtensionsLinq.Filesystem;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;


namespace SongDataLib
{

	public static class SongDataFactory
	{
		public static ISongData LoadFromFile(FileInfo fileObj) { return new SongData(fileObj); }
		public static ISongData LoadFromXElement(XElement xEl) {
			if(xEl.Name == "song") {
				return new SongData(xEl);
			} else if(xEl.Name == "partsong") {
				return new PartialSongData(xEl);
			} else throw new ArgumentException("Don't recognize xml name " + xEl.Name + ", is not a valid ISongData format.", "xEl");
		}

		public static IEnumerable<ISongData> LoadSongList(FileInfo fi) {
			if(fi.Extension == ".xml") { //xmlbased
				XmlReaderSettings settings = new XmlReaderSettings();
				settings.ConformanceLevel = ConformanceLevel.Fragment;
				XmlReader reader = XmlReader.Create(fi.OpenText(), settings);
				while(reader.Read()) {
					if(!reader.IsEmptyElement) continue;
					yield return LoadFromXElement((XElement)XElement.ReadFrom(reader));
				}
			} else {//m3ubased
				TextReader tr;
				if(fi.Extension == ".m3u") tr = new StreamReader(fi.OpenRead(), Encoding.GetEncoding(1252));//open as normal M3U with codepage 1252, and not UTF-8
				else if(fi.Extension == ".m3u8") tr = fi.OpenText();//open as UTF-8
				else throw new ArgumentException("Don't know how to deal with file " + fi.FullName);
				string nextLine = tr.ReadLine();
				bool extm3u = nextLine == "#EXTM3U";
				if(extm3u) nextLine = tr.ReadLine();
				while(nextLine != null) {//read another song!
					if(extm3u) {
						string uri = tr.ReadLine();
						if(uri != null) yield return new PartialSongData(nextLine, uri);
					} else {
						yield return new MinimalSongData(nextLine);
					}
					nextLine = tr.ReadLine();
				}
			}
		}
	}

	public interface ISongData
	{
		string FullInfo { get; }//for searching purposes, should contain all substrings a user is likely to search for (i.e. certainly the track title, perhaps the year released, certainly not the song length in seconds.)
		XElement ConvertToXml();
		string SongUri { get; }//untranslated, mixes URL's and local filesystem path's willy-nilly!
		//bool IsLocal { get; }
		int? Length { get; }
		string HumanLabel { get; }//For display in UI's or so.  This is a fallback, if possible a user should try to use SongData's more complete data, but if that's to no avail...  Must not be null or empty therefore!
	}

	public class MinimalSongData : ISongData
	{
		string songuri;
		bool isLocal;
		public virtual string FullInfo { get { return songuri; } }

		public virtual XElement ConvertToXml() {
			return new XElement("songref", new XAttribute("songuri", songuri));
		}

		public virtual int? Length { get { return null; } }

		public virtual string SongUri { get { return songuri; } }

		public virtual bool IsLocal {
			get { return isLocal; }
		}

		public virtual string HumanLabel {
			get {
				return string.Join("/", songuri.Substring(0, songuri.Length - 4).Split('/', '\\').Reverse().Take(2).Reverse().ToArray());//adhoc best guess.//TODO improve: goes wrong on things like http://whee/boom.mp3#testtest
			}
		}

		public MinimalSongData(string songuri) {
			if(songuri == null || songuri.Length == 0) throw new ArgumentNullException(songuri);
			this.songuri = songuri;
			isLocal = FSUtil.IsValidAbsolutePath(songuri) == true;
		}
		public MinimalSongData(XElement xEl) : this((string)xEl.Attribute("songuri")) { }
	}

	public class PartialSongData : MinimalSongData
	{
		public string label;
		public int? length;

		public override string FullInfo {
			get {
				if(label == null) return SongUri;
				else return SongUri+"\t" + label ;
			}
		}

		public override XElement ConvertToXml() {
			return new XElement("partsong",
				new XAttribute("fileuri", SongUri),
				label == null ? null : new XAttribute("label", label),
				length == null ? null : new XAttribute("length", length.ToStringOrNull())
				);
		}
		public override int? Length { get { return length; } }
		public override string HumanLabel {	get {return label??base.HumanLabel;}}

		static Regex extm3uPattern = new Regex(@"^#EXTINF:(?<songlength>[0-9]+),(?<label>.*)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		internal PartialSongData(XElement xEl) : base((string)xEl.Attribute("fileuri")) {
			label = (string)xEl.Attribute("label");//might even be null!
			length = ParseString.ParseAsInt32((string)xEl.Attribute("length"));//might even be null!
		}
		internal PartialSongData(string extm3ustr, string url): base(url) {
			Match m;
			lock(extm3uPattern) m = extm3uPattern.Match(extm3ustr);
			if(m.Success) {
				length = m.Groups["songlength"].Value.ParseAsInt32();
				if(length == 0) length = null;
				label = m.Groups["label"].Value;
				if(label == "") label = null;
			} else {
				length = null;
				label = null;
				throw new Exception("PartialSongData being constructed from non-EXTM3U string, impossible");
			}
		}
	}

	/// <summary>
	/// Represent all relevent meta-data about a Song.  If this data can't be determined, use PartialSongData instead.
	/// </summary>
	public class SongData:MinimalSongData
	{
		public string filepath, title, artist, performer, composer, album, comment, genre;
		public int? year, track, trackcount, bitrate, length, samplerate, channels;
		public DateTime? lastWriteTime;

		static string makesafe(string data) { return data == null ? data : new string(data.Select(c => c == '\n' || c == '\t' ? ' ' : c).Where(c => c >= ' ').ToArray()); }
		static string makesafe(string[] data) { return data == null ? null : toSafeString(string.Join(", ", data)); }
		static string strNullIfEmpty(string str) { return str == null || str.Length == 0 ? null : str; }

		static string toSafeString(XAttribute data) { return strNullIfEmpty(makesafe((string)data)); }
		static string toSafeString(string data) { return strNullIfEmpty(makesafe(data)); }
		static string toSafeString(string[] data) { return strNullIfEmpty(makesafe(data)); }

		internal SongData(FileInfo fileObj) :base(fileObj.FullName) {
			TagLib.File file = TagLib.File.Create(fileObj.FullName);
			//filepath = toSafeString(file.Name); //is this the same as fileObj.FullName?  yes, as the LocalFileAbstraction class in File.cs shows.
			title = toSafeString(file.Tag.Title);
			artist = toSafeString(file.Tag.Artists);
			performer = toSafeString(file.Tag.Performers);
			composer = toSafeString(file.Tag.Composers);
			album = toSafeString(file.Tag.Album);
			comment = toSafeString(file.Tag.Comment);
			genre = toSafeString(file.Tag.Genres);
			year = (int?)file.Tag.Year;
			track = (int?)file.Tag.Track;
			trackcount = (int?)file.Tag.TrackCount;
			lastWriteTime = fileObj.LastWriteTime;
			bitrate = file.AudioProperties == null ? null : (int?)file.AudioProperties.Bitrate;
			length = file.AudioProperties == null ? null : (int?)file.AudioProperties.Length;
			samplerate = file.AudioProperties == null ? null : (int?)file.AudioProperties.SampleRate;
			channels = file.AudioProperties == null ? null : (int?)file.AudioProperties.Channels;
		}

		internal SongData(XElement from) :base(toSafeString(from.Attribute("filepath"))) {
			//filepath = toSafeString(from.Attribute("filepath"));
			title = toSafeString(from.Attribute("title"));

			artist = toSafeString(from.Attribute("artist"));
			performer = toSafeString(from.Attribute("performer"));
			composer = toSafeString(from.Attribute("composer"));
			album = toSafeString(from.Attribute("album"));
			comment = toSafeString(from.Attribute("comment"));
			genre = toSafeString(from.Attribute("genre"));
			year = SongUtil.StringToNullableInt((string)from.Attribute("year"));
			track = SongUtil.StringToNullableInt((string)from.Attribute("track"));
			trackcount = SongUtil.StringToNullableInt((string)from.Attribute("trackcount"));
			bitrate = SongUtil.StringToNullableInt((string)from.Attribute("bitrate"));
			length = SongUtil.StringToNullableInt((string)from.Attribute("length"));
			samplerate = SongUtil.StringToNullableInt((string)from.Attribute("samplerate"));
			channels = SongUtil.StringToNullableInt((string)from.Attribute("channels"));
			lastWriteTime = ((string)from.Attribute("lastmodified")).ParseAsDateTime();
		}


		public override XElement ConvertToXml() {
			return new XElement("song",
				 filepath == null ? null : new XAttribute("filepath", filepath),
				 title == null ? null : new XAttribute("title", title),
				 artist == null ? null : new XAttribute("artist", artist),
				 performer == null ? null : new XAttribute("performer", performer),
				 composer == null ? null : new XAttribute("composer", composer),
				 album == null ? null : new XAttribute("album", album),
				 comment == null ? null : new XAttribute("comment", comment),
				 genre == null ? null : new XAttribute("genre", genre),
				 year == null ? null : new XAttribute("year", year.ToStringOrNull()),
				 track == null ? null : new XAttribute("track", track.ToStringOrNull()),
				 trackcount == null ? null : new XAttribute("trackcount", trackcount.ToStringOrNull()),
				 bitrate == null ? null : new XAttribute("bitrate", bitrate.ToStringOrNull()),
				 length == null ? null : new XAttribute("length", length.ToStringOrNull()),
				 samplerate == null ? null : new XAttribute("samplerate", samplerate.ToStringOrNull()),
				 channels == null ? null : new XAttribute("channels", channels.ToStringOrNull()),
				 lastWriteTime == null ? null : new XAttribute("lastmodified", lastWriteTime.Value.ToUniversalTime().ToString("u"))
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
				yield return year.ToStringOrNull();
				yield return track.ToStringOrNull();
			}
		}

		public override string FullInfo { get { return string.Join("\t", Values.Where(v => v != null).ToArray()); } }
		public override int? Length { get { return length; } }
		public override string HumanLabel {
			get {
				if(title == null)
					return base.HumanLabel;
				else {
					return
						(album != null ? album + "/" : "") +
						(track != null ? track + " - " : "") +
						(artist != null ? artist + " - " : performer != null ? performer + " - " : "") +
						title;
				}
			}
		}
	}
}