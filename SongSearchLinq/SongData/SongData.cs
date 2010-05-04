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
using System.Text.RegularExpressions;


namespace SongDataLib {


	/// <summary>
	/// Represent all relevent meta-data about a Song.  If this data can't be determined, use PartialSongData instead.
	/// </summary>
	public class SongData : MinimalSongData {
		public string title, performer, composer, album, comment, genre;
		public int year, track, trackcount, bitrate, length, samplerate, channels;
		DateTime m_lastWriteTime;
		public DateTime lastWriteTime { get { return m_lastWriteTime; } set { m_lastWriteTime = value.ToUniversalTime(); } }

		static string strNullIfEmpty(string str) { return str == null || str.Length == 0 ? null : string.Intern(str); }//string.Intern is slow but saves memory for identical strings.

		static string toSafeString(XAttribute data) { return strNullIfEmpty((string)data); }//since it's from XML, no need to verify chars, saves 20% time
		static string toSafeString(string data) { return strNullIfEmpty(Canonicalize.MakeSafe(data)); }

		internal SongData(FileInfo fileObj)
			: base(new Uri(fileObj.FullName, UriKind.Absolute), true) {
			TagLib.File file;
			try {
				file = TagLib.File.Create(fileObj.FullName);
			}
			catch (Exception e) {
				Console.WriteLine("Unable to process " + fileObj.FullName);
				Console.WriteLine("Error type: " + e.GetType().Name);
				if (e.Message != null) Console.WriteLine("Error message: " + e.Message);
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

		//private const string isoDateString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffzz";			//we use an explicit format string so MONO can parse it... it barfed on the auto-recognition.
		//private const Regex isoDateRegex = "([0-9]{4})-([0-9]{2})-([0-9]{2})T([0-9]{2}):([0-9]{2}):([0-9]{2}).([0-9]+)

		internal SongData(XElement from, bool? isLocal)
			: base(from, isLocal) {
			title = (string)from.Attribute("title");

			performer = (string)from.Attribute("performer");//TODO - decide on this, already.
			composer = (string)from.Attribute("composer");//TODO - why bother?
			album = (string)from.Attribute("album");
			comment = (string)from.Attribute("comment");//TODO - why bother?
			genre = (string)from.Attribute("genre");
			year = ((int?)from.Attribute("year")) ?? 0;
			track = ((int?)from.Attribute("track")) ?? 0;
			trackcount = ((int?)from.Attribute("trackcount")) ?? 0;
			bitrate = ((int?)from.Attribute("bitrate")) ?? 0;
			length = ((int?)from.Attribute("length")) ?? 0;
			samplerate = ((int?)from.Attribute("samplerate")) ?? 0;
			channels = ((int?)from.Attribute("channels")) ?? 0;
			long? lastmodifiedTicks = (long?)from.Attribute("lastmodifiedTicks");

			if (lastmodifiedTicks.HasValue)
				lastWriteTime = new DateTime(lastmodifiedTicks.Value, DateTimeKind.Utc);
			//else {
			//    string dateTimeString = (string)from.Attribute("lastmodified");
			//    DateTime timestamp;
			//    if (DateTime.TryParseExact(dateTimeString, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out timestamp))
			//        lastWriteTime = timestamp;
			//}
		}

		public override XElement ConvertToXml(Func<Uri, string> urlTranslator) {
			return new XElement("song",
				 makeUriAttribute(urlTranslator),
				 MakeAttributeOrNull("title", title),
				 MakeAttributeOrNull("performer", performer),
				//TODO rename to "performer", this is just legacy support
				 MakeAttributeOrNull("composer", composer),
				 MakeAttributeOrNull("album", album),
				 MakeAttributeOrNull("comment", comment),
				 MakeAttributeOrNull("genre", genre),
				 MakeAttributeOrNull("year", year),
				 MakeAttributeOrNull("track", track),
				 MakeAttributeOrNull("trackcount", trackcount),
				 MakeAttributeOrNull("bitrate", bitrate),
				 MakeAttributeOrNull("length", length),
				 MakeAttributeOrNull("samplerate", samplerate),
				 MakeAttributeOrNull("channels", channels),
				 MakeAttributeOrNull("lastmodifiedTicks", lastWriteTime == default(DateTime) ? default(long?) : lastWriteTime.Ticks)
				//lastWriteTime == default(DateTime).ToUniversalTime() ? null : new XAttribute("lastmodified", lastWriteTime.ToString("o", CultureInfo.InvariantCulture))
			);
		}

		IEnumerable<string> Values {//only yield "distinctive" search values
			get {
				yield return Uri.UnescapeDataString(SongUri.ToString());
				yield return title;
				yield return performer;
				yield return composer;
				yield return album;
				//yield return comment;
				//yield return genre;
				//yield return year.ToStringOrNull();
				//yield return track.ToStringOrNull();
			}
		}

		public override string FullInfo { get { return string.Join("\n", Values.Where(v => v != null).ToArray()); } }
		public override int Length { get { return length; } }
		public override string HumanLabel {
			get {
				if (title == null)
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