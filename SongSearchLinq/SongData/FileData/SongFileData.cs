using System;
using System.Collections.Generic;

using System.Linq;
using System.Xml.Linq;
using EmnExtensions;
using EmnExtensions.IO;
using EmnExtensions.Text;
using TagLib;

namespace SongDataLib {


	/// <summary>
	/// Represent all relevent meta-data about a Song.  If this data can't be determined, use PartialSongData instead.
	/// </summary>
	public class SongFileData : MinimalSongFileData {
		public readonly string title, artist, composer, album, comment, genre;
		public readonly int year, track, trackcount, bitrate, length, samplerate, channels;
		public readonly double? track_gain;
		public int? rating;
		public uint TrackID;
		public readonly int filesize;
		DateTime m_lastWriteTimeUtc;
		public Popularity popularity = new Popularity { ArtistPopularity = 0, TitlePopularity = 0 };
		public DateTime LastWriteTimeUtc { get { return m_lastWriteTimeUtc; } private set { m_lastWriteTimeUtc = value.ToUniversalTime(); } }

		static string toSafeString(string data) { return Canonicalize.TrimAndMakeSafe(data); }


		internal SongFileData(Uri baseUri, LFile fileObj, IPopularityEstimator popEst)
			: base(baseUri, new Uri(fileObj.FullName, UriKind.Absolute), true) {
			IAudioCodec properties;
			ILookup<string, string> customtags;
			Tag tag;
			GetTag(fileObj, out properties, out customtags, out tag);

			title = toSafeString(tag.Title);
			artist = toSafeString(tag.JoinedPerformers);
			composer = toSafeString(tag.JoinedComposers);
			album = toSafeString(tag.Album);
			comment = toSafeString(tag.Comment);
			genre = toSafeString(tag.JoinedGenres);
			year = (int)tag.Year;
			track = (int)tag.Track;
			trackcount = (int)tag.TrackCount;
			LastWriteTimeUtc = fileObj.LastWriteTime;
			filesize = (int)fileObj.Length;
			bitrate = properties == null ? 0 : properties.AudioBitrate;
			length = properties == null ? 0 : (int)Math.Round(properties.Duration.TotalSeconds);
			samplerate = properties == null ? 0 : properties.AudioSampleRate;
			channels = properties == null ? 0 : properties.AudioChannels;

			rating =
				customtags["rating"]
				.Select(ratingStr => ratingStr.ParseAsInt32())
				.Distinct().Max();
			track_gain =
				customtags["replaygain_track_gain"]
					.Select(trackgainStr => trackgainStr.Replace("dB", "").Replace("db", "").Trim().ParseAsDouble())
					.Where(trackgainVal => trackgainVal.HasValue)
					.Distinct().SingleOrDefault();

			popularity = popEst == null ? default(Popularity) : popEst.EstimatePopularity(artist, title);
		}

		static void GetTag(LFile fileObj, out IAudioCodec properties, out ILookup<string, string> customtags, out Tag tag) {
			//if (fileObj.Extension.ToLowerInvariant() == ".mp3") {
			//    prefer reading only start of file.
			//    taglib doesn't really support this, unfortunately.
			//}
			
			TagLib.File file = TagLib.File.Create(fileObj.FullName);
			properties = file.Properties;
			tag = file.Tag;
			customtags = GetCustomTags(file);
		}

		static ILookup<string, string> GetCustomTags(TagLib.File file) {
			return GetCustomTagsList(file).ToLookup(keyval => keyval.Item1.ToLowerInvariant(), keyval => keyval.Item2);
		}

		static IEnumerable<Tuple<string, string>> GetCustomTagsList(TagLib.File file) {
			TagTypes types = file.TagTypes;
			if (types.HasFlag(TagTypes.Xiph)) {
				var filetag = (file.GetTag(TagTypes.Xiph, false) as TagLib.Ogg.XiphComment);
				return filetag.SelectMany(key => filetag.GetField(key).Select(val => Tuple.Create(key, val)));
			} else if (types.HasFlag(TagTypes.Id3v2)) {
				return UserTextInformationFrames(file)
					.SelectMany(frame => frame.Text.Select(val => Tuple.Create(frame.Description, val)));
			} else if (types.HasFlag(TagTypes.Ape)) {
				var filetag = (file.GetTag(TagTypes.Ape, false) as TagLib.Ape.Tag);
				return filetag.SelectMany(key => filetag.GetItem(key).ToStringArray().Select(val => Tuple.Create(key, val)));
			} else if (types == TagTypes.None || types == TagTypes.Id3v1) {
				return Enumerable.Empty<Tuple<string, string>>();
			} else if (types.HasFlag(TagTypes.Asf)) {
				var filetag = file.GetTag(TagTypes.Asf, false) as TagLib.Asf.Tag;
				return filetag.Where(desc => desc.Type == TagLib.Asf.DataType.Unicode)
					.Select(key => Tuple.Create(key.Name, key.ToString()));
			} else
				throw new NotImplementedException();
		}

		private static IEnumerable<TagLib.Id3v2.UserTextInformationFrame> UserTextInformationFrames(TagLib.File file) {
			return ((TagLib.Id3v2.Tag)file.GetTag(TagTypes.Id3v2, false))
				.GetFrames("TXXX").Cast<TagLib.Id3v2.UserTextInformationFrame>();
		}

		public void WriteRatingToFile() {
			var file = TagLib.File.Create(SongUri.LocalPath);
			TagTypes types = file.TagTypes & (~TagTypes.Id3v1);
			if (types.HasFlag(TagTypes.Xiph) || file is TagLib.Ogg.File) {
				var filetag = (file.GetTag(TagTypes.Xiph, true) as TagLib.Ogg.XiphComment);
				if (rating == null)
					filetag.RemoveField("rating");//case insensitive
				else
					filetag.SetField("rating", rating.Value.ToString());

				types = types & (~TagTypes.Xiph);
			}

			if (types.HasFlag(TagTypes.Id3v2) || file is TagLib.Mpeg.AudioFile) {
				var filetag = ((TagLib.Id3v2.Tag)file.GetTag(TagTypes.Id3v2, true));
				var ratingfields = UserTextInformationFrames(file).Where(frame => frame.Description.ToLowerInvariant() == "rating").ToArray();
				foreach (var fieldtodelete in ratingfields.Where(frame => frame.Description != "rating" || rating == null))
					filetag.RemoveFrame(fieldtodelete);

				if (rating != null)
					TagLib.Id3v2.UserTextInformationFrame.Get(filetag, "rating", true).Text = new[] { rating.Value.ToString() };
				types = types & (~TagTypes.Id3v2);
			}

			if (types.HasFlag(TagTypes.Ape)) {
				var filetag = (file.GetTag(TagTypes.Ape, false) as TagLib.Ape.Tag);
				if (rating == null)
					filetag.RemoveItem("rating");//case insensitive
				else
					filetag.SetValue("rating", rating.Value.ToString());
				types = types & (~TagTypes.Ape);
			}

			if (types.HasFlag(TagTypes.Asf)) {
				var filetag = file.GetTag(TagTypes.Asf, false) as TagLib.Asf.Tag;
				if (rating == null)
					filetag.RemoveDescriptors("rating");
				filetag.SetDescriptorString(rating.Value.ToString(), "rating");//note reversed order!
				types = types & (~TagTypes.Asf);
			}

			if (types != TagTypes.None)
				throw new NotImplementedException();
			file.Save();
		}

		//faster to not recreate XNames.
		readonly static XName songN = "song", titleN = "title", artistN = "artist", performerN = "performer", composerN = "composer", albumN = "album",
			commentN = "comment", genreN = "genre", yearN = "year", trackN = "track", trackcountN = "trackcount", bitrateN = "bitrate",
			lengthN = "length", samplerateN = "samplerate", channelsN = "channels", lastmodifiedTicksN = "lastmodifiedTicks",
			ratingN = "rating", artistpopularityN = "popA", titlepopularityN = "popT", trackGainN = "Tgain", filesizeN = "filesize";

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
			if (lastmodifiedTicks.HasValue) LastWriteTimeUtc = new DateTime(lastmodifiedTicks.Value, DateTimeKind.Utc);
			filesize = (int?)from.Attribute(filesizeN) ?? (IsLocal ? (int)new LFile(SongUri.LocalPath).Length : 0);
		}

		public SongFileData(Uri baseUri, Uri uri, string artist, string title, int? length, int? rating, double? replaygain)
			: base(baseUri, uri, false) {
			this.artist = artist;
			this.title = title;
			this.length = length??0;
			this.rating = rating;
			this.track_gain = replaygain;
		}


		public double popA_forscripting { get { return Math.Sqrt(popularity.ArtistPopularity / 350000.0); } }
		public double popT_forscripting { get { return popularity.TitlePopularity / Math.Max(1.0, popularity.ArtistPopularity * 0.95 + 0.05 * 365000); } }

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
				 ? MakeAttributeOrNull(artistpopularityN, popA_forscripting)
				 : MakeAttributeOrNull(artistpopularityN, popularity.ArtistPopularity),
				 coreOnly
				 ? MakeAttributeOrNull(titlepopularityN, popT_forscripting)
				 : MakeAttributeOrNull(titlepopularityN, popularity.TitlePopularity),
				 coreOnly ? null : MakeAttributeOrNull(lastmodifiedTicksN, LastWriteTimeUtc == default(DateTime) ? default(long?) : LastWriteTimeUtc.Ticks),
				 coreOnly ? null : MakeAttributeOrNull(filesizeN, filesize)
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
		public override double AverageBitrate { get { return filesize * 8.0 / length; } }
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
		public override IEnumerable<SongRef> PossibleSongs { get { return artist != null && title != null ? Enumerable.Repeat(SongRef.Create(artist, title), 1) : base.PossibleSongs; } }
	}
}