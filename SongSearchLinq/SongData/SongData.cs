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
		public string title, artist, composer, album, comment, genre;
		public int year, track, trackcount, bitrate, length, samplerate, channels;
		public int? rating;
		DateTime m_lastWriteTime;
		//public Popularity popularity = new Popularity { ArtistPopularity = 0, TitlePopularity = 0 };
		public DateTime lastWriteTime { get { return m_lastWriteTime; } set { m_lastWriteTime = value.ToUniversalTime(); } }

		static string strNullIfEmpty(string str) { return str == null || str.Length == 0 ? null : str; }//string.Intern is slow but saves memory for identical strings.

		static string toSafeString(string data) { return strNullIfEmpty(Canonicalize.MakeSafe(data)); }

		internal SongData(FileInfo fileObj)
			: base(new Uri(fileObj.FullName, UriKind.Absolute), true) {
			TagLib.File file;
			file = TagLib.File.Create(fileObj.FullName);
			title = toSafeString(file.Tag.Title);
			artist = toSafeString(file.Tag.JoinedPerformers);
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
			rating = GetRatingFrom(file);
		}

		static int? GetRatingFrom(TagLib.File file) {
			TagLib.TagTypes types = file.TagTypes;
			if (types.HasFlag(TagLib.TagTypes.Xiph)) {
				var tag = file.GetTag(TagLib.TagTypes.Xiph, false) as TagLib.Ogg.XiphComment;
				return tag.GetField("RATING").FirstOrDefault().ParseAsInt32();
			} else if (types.HasFlag(TagLib.TagTypes.Id3v2)) {
				var foobarRating = ((TagLib.Id3v2.Tag)file.GetTag(TagLib.TagTypes.Id3v2, false)).GetFrames("TXXX").Cast<TagLib.Id3v2.UserTextInformationFrame>().Where(uf => uf.Description.ToLowerInvariant() == "rating").FirstOrDefault();
				return foobarRating == null ? default(int?) : foobarRating.Text.First().ParseAsInt32();
			} else if (types == TagLib.TagTypes.None) {
				return default(int?);
			} else if (types.HasFlag(TagLib.TagTypes.Ape)) {
				var foobarRating = (file.GetTag(TagLib.TagTypes.Ape, false) as TagLib.Ape.Tag).GetItem("rating");
				return foobarRating == null ? default(int?) : foobarRating.ToStringArray().First().ParseAsInt32();
			} else {
				throw new NotImplementedException();
			}
		}

		//faster to not recreate XNames.
		static XName songN = "song", titleN = "title", artistN = "artist", performerN = "performer", composerN = "composer", albumN = "album", commentN = "comment", genreN = "genre", yearN = "year", trackN = "track", trackcountN = "trackcount", bitrateN = "bitrate", lengthN = "length", samplerateN = "samplerate", channelsN = "channels", lastmodifiedTicksN = "lastmodifiedTicks", ratingN = "rating", artistpopularityN = "popA", titlepopularityN = "popT";

		internal SongData(XElement from, bool? isLocal)
			: base(from, isLocal) {
			title = (string)from.Attribute(titleN);

			artist = (string)from.Attribute(artistN) ?? (string)from.Attribute(performerN);
			composer = (string)from.Attribute(composerN);
			album = (string)from.Attribute(albumN);
			comment = (string)from.Attribute(commentN);
			genre = (string)from.Attribute(genreN);
			year = ((int?)from.Attribute(yearN)) ?? 0;
			track = ((int?)from.Attribute(trackN)) ?? 0;
			trackcount = ((int?)from.Attribute(trackcountN)) ?? 0;
			bitrate = ((int?)from.Attribute(bitrateN)) ?? 0;
			length = ((int?)from.Attribute(lengthN)) ?? 0;
			samplerate = ((int?)from.Attribute(samplerateN)) ?? 0;
			channels = ((int?)from.Attribute(channelsN)) ?? 0;
			rating = (int?)from.Attribute(ratingN);
			//popularity.ArtistPopularity = ((int?)from.Attribute(artistpopularityN)) ?? 0;
			//popularity.TitlePopularity = ((int?)from.Attribute(titlepopularityN)) ?? 0;

			long? lastmodifiedTicks = (long?)from.Attribute(lastmodifiedTicksN);
			if (lastmodifiedTicks.HasValue) lastWriteTime = new DateTime(lastmodifiedTicks.Value, DateTimeKind.Utc);

		}

		public override XElement ConvertToXml(Func<Uri, string> urlTranslator) {
			return new XElement(songN,
				 makeUriAttribute(urlTranslator),
				 MakeAttributeOrNull(titleN, title),
				 MakeAttributeOrNull(artistN, artist),
				//TODO rename to NperformerN, this is just legacy support
				 MakeAttributeOrNull(composerN, composer),
				 MakeAttributeOrNull(albumN, album),
				 MakeAttributeOrNull(commentN, comment),
				 MakeAttributeOrNull(genreN, genre),
				 MakeAttributeOrNull(yearN, year),
				 MakeAttributeOrNull(trackN, track),
				 MakeAttributeOrNull(trackcountN, trackcount),
				 MakeAttributeOrNull(bitrateN, bitrate),
				 MakeAttributeOrNull(lengthN, length),
				 MakeAttributeOrNull(samplerateN, samplerate),
				 MakeAttributeOrNull(channelsN, channels),
				 MakeAttributeOrNull(ratingN, rating),
				 //MakeAttributeOrNull(artistpopularityN, popularity.ArtistPopularity),
				 //MakeAttributeOrNull(titlepopularityN, popularity.TitlePopularity),
				 MakeAttributeOrNull(lastmodifiedTicksN, lastWriteTime == default(DateTime) ? default(long?) : lastWriteTime.Ticks)
			);
		}

		IEnumerable<string> Values {//only yield "distinctive" search values
			get {
				yield return Uri.UnescapeDataString(SongUri.ToString());
				yield return title;
				yield return artist;
				yield return composer;
				yield return album;
				//yield return comment;
				yield return genre;
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
						(artist != null ? artist.TrimEnd() + " - " : "") +
						title.TrimEnd();
				}
			}
		}
	}
}