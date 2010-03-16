using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using EmnExtensions;
using System.Text;
using EmnExtensions.Text;
using System.Web;


namespace SongDataLib
{


	/// <summary>
	/// Represent all relevent meta-data about a Song.  If this data can't be determined, use PartialSongData instead.
	/// </summary>
	public class SongData : MinimalSongData
	{
		public string title, performer, composer, album, comment, genre;
		public int year, track, trackcount, bitrate, length, samplerate, channels;
		public DateTime lastWriteTime;

		static string strNullIfEmpty(string str) { return str == null || str.Length == 0 ? null : string.Intern(str); }//string.Intern is slow but saves memory for identical strings.

		static string toSafeString(XAttribute data) { return strNullIfEmpty((string)data); }//since it's from XML, no need to verify chars, saves 20% time
		static string toSafeString(string data) { return strNullIfEmpty(Canonicalize.MakeSafe(data)); }

		internal SongData(FileInfo fileObj)
			: base(fileObj.FullName, true) {
			TagLib.File file;
			try {
				file = TagLib.File.Create(fileObj.FullName);
			} catch(Exception e) {
				Console.WriteLine("Unable to process " + fileObj.FullName);
				Console.WriteLine("Error type: "+e.GetType().Name);
				if(e.Message != null) Console.WriteLine("Error message: " + e.Message);
				throw;
			}
			title = toSafeString(file.Tag.Title);
			performer = toSafeString(file.Tag.JoinedPerformers);
			composer = toSafeString(file.Tag.JoinedComposers);
			album = toSafeString(file.Tag.Album);
			comment = toSafeString(file.Tag.Comment);
			genre = toSafeString(file.Tag.JoinedGenres);
			year = (int)file.Tag.Year;
			track = (int)file.Tag.Track;
			trackcount = (int)file.Tag.TrackCount;
			lastWriteTime = fileObj.LastWriteTime;
			bitrate = file.Properties == null ? 0 : file.Properties.AudioBitrate;
			length = file.Properties == null ? 0 : (int)Math.Round(file.Properties.Duration.TotalSeconds);
			samplerate = file.Properties == null ? 0 : file.Properties.AudioSampleRate;
			channels = file.Properties == null ? 0 : file.Properties.AudioChannels;
		}

		private const string isoDateString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffzz";			//we use an explicit format string so MONO can parse it... it barfed on the auto-recognition.


		internal SongData(XElement from, bool? isLocal)
			: base(from, isLocal) {
			title = toSafeString(from.Attribute("title"));

			performer = toSafeString(from.Attribute("performer") ?? from.Attribute("artist"));//TODO - decide on this, already.
			composer = toSafeString(from.Attribute("composer"));//TODO - why bother?
			album = toSafeString(from.Attribute("album"));
			comment = toSafeString(from.Attribute("comment"));//TODO - why bother?
			genre = toSafeString(from.Attribute("genre"));
			year = ParseInt((string)from.Attribute("year"));
			track = ParseInt((string)from.Attribute("track"));
			trackcount = ParseInt((string)from.Attribute("trackcount"));
			bitrate = ParseInt((string)from.Attribute("bitrate"));
			length = ParseInt((string)from.Attribute("length"));
			samplerate = ParseInt((string)from.Attribute("samplerate"));
			channels = ParseInt((string)from.Attribute("channels"));
			string dateTimeString = (string)from.Attribute("lastmodified");
			DateTime.TryParseExact(dateTimeString,isoDateString , null, DateTimeStyles.None, out lastWriteTime);
		}

		internal static int ParseInt(string num) {
			int retval;
			int.TryParse(num, out retval);//used to deal with nullables, but no longer necessary...
			return retval;
		}
		public override XElement ConvertToXml(Func<string, string> urlTranslator) {
			return new XElement("song",
				 makeUriAttribute(urlTranslator),
				 title == null ? null : new XAttribute("title", title),
				 performer == null ? null : new XAttribute("artist", performer),
				 //TODO rename to "performer", this is just legacy support
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
				lastWriteTime == default(DateTime) ? null : new XAttribute("lastmodified", lastWriteTime.ToString(isoDateString))
			);
		}

		public IEnumerable<string> Values {//only yield "distinctive" search values
			get {
				yield return Uri.IsWellFormedUriString(songuri,UriKind.RelativeOrAbsolute)? Uri.UnescapeDataString(songuri):songuri;
				yield return title;
				yield return performer;
				yield return composer;
				yield return album;
				yield return comment;
				yield return genre;
				yield return year.ToStringOrNull();
				yield return track.ToStringOrNull();
			}
		}

		public override string FullInfo { get { return string.Join("\n", Values.Where(v => v != null).ToArray()); } }
		public override int Length { get { return length; } }
		public override string HumanLabel {
			get {
				if(title == null)
					return base.HumanLabel;
				else {
					return
						(performer != null ? performer.TrimEnd() + " - " : "") +
						title.TrimEnd();
				}
			}
		}
	}
}