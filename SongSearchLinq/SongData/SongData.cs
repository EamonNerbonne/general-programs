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
	}

	public interface ISongData
	{
		/// <summary>
		/// String representation of all meta-data a user is likely to search for.   This will be indexed for searching purposes - i.e it should include certainly the track title,
		/// and perhaps the year released, but certainly not the song length in seconds.
		/// </summary>
		string FullInfo { get; }

		/// <summary>
		/// Converts to xml.  The class should be able to load from xml too then, and supply the appropriate constructor.
		/// </summary>
		/// <returns>A sufficiently complete XML representation such that the object could load from it.</returns>
		XElement ConvertToXml();
		/// <summary>
		/// The path to the song.  This property mixes local path's and remote uri's, to differentiate, use the IsLocal Property.
		/// </summary>
		string SongPath { get; }//untranslated, mixes URL's and local filesystem path's willy-nilly!
		/// <summary>
		/// This is a security-sensitive property!
		/// Returns whether this song is a local song.  A local song's SongPath property will potentially be resolved and the song file it points to used.
		/// </summary>
		bool IsLocal { get; }
		/// <summary>
		/// The length of the song in seconds.
		/// </summary>
		int? Length { get; }
		/// <summary>
		/// As best as possible, a human-readable version of the meta-data concerning the song.  This is for display in GUI's or so, and thus doesn't need to be as complete as FullInfo.  Must not be null or empty therefore!
		/// This data is a fallback, if possible a user interface should try to use SongData's (or any other implementing class's) more complete data, but if that's to no avail...  
		/// </summary>
		string HumanLabel { get; }
	}

	public class MinimalSongData : ISongData
	{
		protected string songuri;
		protected bool isLocal;
		public virtual string FullInfo { get { return songuri; } }

		public virtual XElement ConvertToXml() {
			return new XElement("songref", new XAttribute("songuri", songuri));
		}

		public virtual int? Length { get { return null; } }

		public virtual string SongPath { get { return songuri; } }

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
				if(label == null) return SongPath;
				else return SongPath+"\t" + label ;
			}
		}

		public override XElement ConvertToXml() {
			return new XElement("partsong",
				new XAttribute("fileuri", SongPath),
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
		public string  title, artist, performer, composer, album, comment, genre;
		public int year, track, trackcount, bitrate, length, samplerate, channels;
		public DateTime lastWriteTime;

		static string makesafe(string data) { return data == null? data : new string(data.Select(c => c == '\n' || c == '\t' ? ' ' : c).Where(c => c >= ' ').ToArray()); }
		/*static string makesafe(string data) {
			int strLen = data.Length;
			char[] str = null;
			for(int i = 0; i < strLen; i++) {
				char c = data[i];
				if(c == '\n' || c == '\t' || c < ' ') {
					str = str ?? data.ToCharArray();
					str[i] = ' ';
				}
			}
			return str == null ? data : new string(str);
		}
*/
		static string makesafe(string[] data) { return data == null || data.Length == 0 || data.All(s => s.Length == 0) ? null : toSafeString(string.Join(", ", data)); }
		static string strNullIfEmpty(string str) { return str == null || str.Length == 0 ? null : str; }

		static string toSafeString(XAttribute data) { return strNullIfEmpty((string)data); }//since it's from XML, no need to verify chars, saves 20% time
		static string toSafeString(string data) { return strNullIfEmpty(makesafe(data)); }
		static string toSafeString(string[] data) { return makesafe(data); }

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
			year = (int) file.Tag.Year;
			track = (int)file.Tag.Track;
			trackcount = (int)file.Tag.TrackCount;
			lastWriteTime = fileObj.LastWriteTime;
			bitrate = file.AudioProperties == null ? 0 : file.AudioProperties.Bitrate;
			length = file.AudioProperties == null ? 0 : file.AudioProperties.Length;
			samplerate = file.AudioProperties == null ? 0 : file.AudioProperties.SampleRate;
			channels = file.AudioProperties == null ? 0 : file.AudioProperties.Channels;
		}

		internal SongData(XElement from) : base(toSafeString(from.Attribute("filepath"))) {
			//filepath = toSafeString(from.Attribute("filepath"));
			title = toSafeString(from.Attribute("title"));

			artist = toSafeString(from.Attribute("artist"));
			performer = toSafeString(from.Attribute("performer"));
			composer = toSafeString(from.Attribute("composer"));
			album = toSafeString(from.Attribute("album"));
			comment = toSafeString(from.Attribute("comment"));
			genre = toSafeString(from.Attribute("genre"));
			year = ParseInt((string)from.Attribute("year"));
			track = ParseInt((string)from.Attribute("track"));
			trackcount = ParseInt((string)from.Attribute("trackcount"));
			bitrate = ParseInt((string)from.Attribute("bitrate"));
			length = ParseInt((string)from.Attribute("length"));
			samplerate = ParseInt((string)from.Attribute("samplerate"));
			channels = ParseInt((string)from.Attribute("channels"));
			lastWriteTime = ((string)from.Attribute("lastmodified")).ParseAsDateTime() ?? default(DateTime);//pretty slow. might be faster with explicit format.
		}

		internal static int ParseInt(string num) {
			int retval;
			int.TryParse(num, out retval);//used to deal with nullables, but no longer necessary...
			return retval;
		}

		public override XElement ConvertToXml() {
			return new XElement("song",
				 songuri == null ? null : new XAttribute("filepath", songuri),
				 title == null ? null : new XAttribute("title", title),
				 artist == null ? null : new XAttribute("artist", artist),
				 performer == null ? null : new XAttribute("performer", performer),
				 composer == null ? null : new XAttribute("composer", composer),
				 album == null ? null : new XAttribute("album", album),
				 comment == null ? null : new XAttribute("comment", comment),
				 genre == null ? null : new XAttribute("genre", genre),
				 year == 0 ? null : new XAttribute("year", year.ToStringOrNull()),
				 track == 0 ? null : new XAttribute("track", track.ToStringOrNull()),
				 trackcount == 0 ? null : new XAttribute("trackcount", trackcount.ToStringOrNull()),
				 bitrate == 0 ? null : new XAttribute("bitrate", bitrate.ToStringOrNull()),
				 length == 0 ? null : new XAttribute("length", length.ToStringOrNull()),
				 samplerate == 0 ? null : new XAttribute("samplerate", samplerate.ToStringOrNull()),
				 channels == 0 ? null : new XAttribute("channels", channels.ToStringOrNull()),
				 lastWriteTime == default(DateTime) ? null : new XAttribute("lastmodified", lastWriteTime.ToUniversalTime().ToString("o"))
			);
		}

		public IEnumerable<string> Values {//only yield "distinctive search values
			get {
				yield return songuri;
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
						(track != 0 ? track + " - " : "") +
						(artist != null ? artist + " - " : performer != null ? performer + " - " : "") +
						title;
				}
			}
		}
	}
}