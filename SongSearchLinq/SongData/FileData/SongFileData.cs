using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using EmnExtensions;
using EmnExtensions.Text;
using TagLib.Id3v2;


namespace SongDataLib {


	/// <summary>
	/// Represent all relevent meta-data about a Song.  If this data can't be determined, use PartialSongData instead.
	/// </summary>
	public class SongFileData : MinimalSongFileData {
		public readonly string title, artist, composer, album, comment, genre;
		public readonly int year, track, trackcount, bitrate, length, samplerate, channels;
		public readonly double? track_gain;
		public int? rating;
		DateTime m_lastWriteTime;
		public Popularity popularity = new Popularity { ArtistPopularity = 0, TitlePopularity = 0 };
		public DateTime lastWriteTime { get { return m_lastWriteTime; } private set { m_lastWriteTime = value.ToUniversalTime(); } }

		static string toSafeString(string data) { return Canonicalize.TrimAndMakeSafe(data); }

		internal SongFileData(Uri baseUri, FileInfo fileObj, IPopularityEstimator popEst)
			: base(baseUri, new Uri(fileObj.FullName, UriKind.Absolute), true) {
			TagLib.File file = TagLib.File.Create(fileObj.FullName);
			var customtags = GetCustomTags(file);
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
			rating = customtags.ContainsKey("rating") ? customtags["rating"].ParseAsInt32() : null;
			string track_gain_str;
			if (customtags.TryGetValue("replaygain_track_gain", out track_gain_str))
				track_gain_str = track_gain_str.Replace("dB", "").Replace("db", "").Trim();
			track_gain = track_gain_str.ParseAsDouble();

			popularity = popEst == null ? default(Popularity) : popEst.EstimatePopularity(artist, title);
		}

		static Dictionary<string, string> GetCustomTags(TagLib.File file) {
			TagLib.TagTypes types = file.TagTypes;
			if (types.HasFlag(TagLib.TagTypes.Xiph)) {
				var filetag = (file.GetTag(TagLib.TagTypes.Xiph, false) as TagLib.Ogg.XiphComment);
				return filetag.ToDictionary(key => key.ToLowerInvariant(), key => filetag.GetField(key).First());
			} else if (types.HasFlag(TagLib.TagTypes.Id3v2)) {
				return ((Tag)file.GetTag(TagLib.TagTypes.Id3v2, false))
					.GetFrames("TXXX").Cast<UserTextInformationFrame>()
					.ToDictionary(frame => frame.Description.ToLowerInvariant(), frame => frame.Text.FirstOrDefault());
			} else if (types.HasFlag(TagLib.TagTypes.Ape)) {
				var filetag = (file.GetTag(TagLib.TagTypes.Ape, false) as TagLib.Ape.Tag);
				return filetag.ToDictionary(key => key.ToLowerInvariant(), key => filetag.GetItem(key).ToStringArray().FirstOrDefault());
			} else if (types == TagLib.TagTypes.None || types == TagLib.TagTypes.Id3v1) {
				return new Dictionary<string, string>();
			} else if (types.HasFlag(TagLib.TagTypes.Asf)) {
				var filetag = file.GetTag(TagLib.TagTypes.Asf, false) as TagLib.Asf.Tag;
				return filetag.Where(desc => desc.Type == TagLib.Asf.DataType.Unicode)
					.ToDictionary(key => key.Name.ToLowerInvariant(), key => key.ToString());
			} else
				throw new NotImplementedException();
		}

		public void WriteRatingToFile() {
			var file = TagLib.File.Create(SongUri.LocalPath);
			TagLib.TagTypes types = file.TagTypes;
			if (types.HasFlag(TagLib.TagTypes.Xiph) || file is TagLib.Ogg.File) {
				var filetag = (file.GetTag(TagLib.TagTypes.Xiph, true) as TagLib.Ogg.XiphComment);
				if(rating==null)
					filetag.RemoveField("rating");
				else
					filetag.SetField("rating", rating.Value.ToString());
				
			} else if (types.HasFlag(TagLib.TagTypes.Id3v2)) {
				var filetag = ((Tag)file.GetTag(TagLib.TagTypes.Id3v2, true));
				var ratingfield=UserTextInformationFrame.Get(filetag, "rating", true);
				if (rating == null)
					filetag.RemoveFrame(ratingfield);
				else
					ratingfield.Text = new[]{rating.Value.ToString()};
			} else if (types.HasFlag(TagLib.TagTypes.Ape)) {
				var filetag = (file.GetTag(TagLib.TagTypes.Ape, false) as TagLib.Ape.Tag);
				if (rating == null)
					filetag.RemoveItem("rating");
				else
					filetag.SetValue("rating", rating.Value.ToString());
			} else if (types == TagLib.TagTypes.None || types == TagLib.TagTypes.Id3v1) {
				//return new Dictionary<string, string>();
			} else if (types.HasFlag(TagLib.TagTypes.Asf)) {
				var filetag = file.GetTag(TagLib.TagTypes.Asf, false) as TagLib.Asf.Tag;
				if(rating==null)
					filetag.RemoveDescriptors("rating");
				filetag.SetDescriptorString(rating.Value.ToString(), "rating");//note reversed order!
			} else
				throw new NotImplementedException();
			file.Save();
		}

		//faster to not recreate XNames.
		readonly static XName songN = "song", titleN = "title", artistN = "artist", performerN = "performer", composerN = "composer", albumN = "album",
			commentN = "comment", genreN = "genre", yearN = "year", trackN = "track", trackcountN = "trackcount", bitrateN = "bitrate",
			lengthN = "length", samplerateN = "samplerate", channelsN = "channels", lastmodifiedTicksN = "lastmodifiedTicks",
			ratingN = "rating", artistpopularityN = "popA", titlepopularityN = "popT", trackGainN = "Tgain";

		internal SongFileData(Uri baseUri, XElement from, bool? isLocal, IPopularityEstimator popEst)
			: base(baseUri, from, isLocal) {
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
			track_gain = (double?)from.Attribute(trackGainN);
			int? aPop = (int?)from.Attribute(artistpopularityN);
			int? tPop = (int?)from.Attribute(titlepopularityN);

			//if (aPop.HasValue || popEst == null) {
			popularity.ArtistPopularity = aPop ?? 0;
			popularity.TitlePopularity = tPop ?? 0;
			//} else {
			//    popularity = popEst.EstimatePopularity(artist, title);
			//}

			long? lastmodifiedTicks = (long?)from.Attribute(lastmodifiedTicksN);
			if (lastmodifiedTicks.HasValue) lastWriteTime = new DateTime(lastmodifiedTicks.Value, DateTimeKind.Utc);

		}

		public override XElement ConvertToXml(Func<Uri, string> urlTranslator, bool coreOnly) {
			return new XElement(songN,
				 makeUriAttribute(urlTranslator),
				 MakeAttributeOrNull(titleN, title),
				 MakeAttributeOrNull(artistN, artist),
				 coreOnly ? null : MakeAttributeOrNull(composerN, composer),
				 MakeAttributeOrNull(albumN, album),
				 coreOnly ? null : MakeAttributeOrNull(commentN, comment),
				 coreOnly ? null : MakeAttributeOrNull(genreN, genre),
				 coreOnly ? null : MakeAttributeOrNull(yearN, year),
				 MakeAttributeOrNull(trackN, track),
				 coreOnly ? null : MakeAttributeOrNull(trackcountN, trackcount),
				 coreOnly ? null : MakeAttributeOrNull(bitrateN, bitrate),
				 MakeAttributeOrNull(lengthN, length),
				 coreOnly ? null : MakeAttributeOrNull(samplerateN, samplerate),
				 coreOnly ? null : MakeAttributeOrNull(channelsN, channels),
				 coreOnly && artist == null ? new XAttribute("label", HumanLabel) : null,
				 MakeAttributeOrNull(ratingN, rating),
				 MakeAttributeOrNull(trackGainN, track_gain),
				 coreOnly
				 ? MakeAttributeOrNull(artistpopularityN, Math.Sqrt(popularity.ArtistPopularity / 350000.0))
				 : MakeAttributeOrNull(artistpopularityN, popularity.ArtistPopularity),
				 coreOnly
				 ? MakeAttributeOrNull(titlepopularityN, popularity.TitlePopularity / Math.Max(1.0, popularity.ArtistPopularity * 0.95 + 0.05 * 365000))
				 : MakeAttributeOrNull(titlepopularityN, popularity.TitlePopularity),
				 coreOnly ? null : MakeAttributeOrNull(lastmodifiedTicksN, lastWriteTime == default(DateTime) ? default(long?) : lastWriteTime.Ticks)
			);
		}

		static readonly char[] seps = new[] { '\\' };
		IEnumerable<string> Values {//only yield "distinctive" search values
			get {
				if (baseUri != null && SongUri.OriginalString.StartsWith(baseUri.OriginalString))
					yield return SongUri.OriginalString.Substring(baseUri.OriginalString.Length).TrimStart(seps);
				else
					yield return SongUri.OriginalString;
				//yield return baseUri == null || !SongUri.LocalPath.StartsWith(baseUri.LocalPath) ? SongUri.LocalPath : SongUri.LocalPath.Substring(baseUri.LocalPath.Length).TrimStart(seps);
				yield return title;
				yield return artist;
				//yield return composer;
				yield return album;
				//yield return comment;
				//yield return genre;
				//yield return year.ToStringOrNull();
				yield return track.ToStringOrNull();
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