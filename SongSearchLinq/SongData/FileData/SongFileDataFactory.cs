using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using EmnExtensions;
using EmnExtensions.IO;

namespace SongDataLib {
	public delegate void SongDataLoadDelegate(ISongFileData newsong, double estimatedCompletion);
	public static class SongFileDataFactory {
		public static ISongFileData ConstructFromFile(Uri baseUri, LFile fileObj, IPopularityEstimator popEst) { return new SongFileData(baseUri, fileObj, popEst); }
		public static ISongFileData ConstructFromXElement(Uri baseUri, XElement xEl, bool? isLocal, IPopularityEstimator popEst) {
			if (xEl.Name == "song") {
				return new SongFileData(baseUri, xEl, isLocal, popEst);
			} else if (xEl.Name == "partsong") {
				return new PartialSongFileData(baseUri, xEl, isLocal);
			} else if (xEl.Name == "songref") {
				return new MinimalSongFileData(baseUri, xEl, isLocal);
			} else
				throw new ArgumentException("Don't recognize xml name " + xEl.Name + ", is not a valid ISongData format.", "xEl");
		}

		public static void LoadSongsFromXmlFrag(Uri baseUri, Stream songSource, SongDataLoadDelegate songSink, bool? songsLocal, IPopularityEstimator popEst) {
			long streamLength = F.Swallow(() => songSource.Length, () => -1);
			BlockingCollection<Tuple<XElement, long>> els = new BlockingCollection<Tuple<XElement, long>>();
			Task.Factory.StartNew(() => {
				//XmlReaderSettings settings = new XmlReaderSettings();
				//settings.ConformanceLevel = ConformanceLevel.Fragment;
				using (var textreader = new StreamReader(songSource))
				using (var reader = XmlReader.Create(textreader)) {
					while (reader.Read()) {
						if (!reader.IsEmptyElement || !reader.HasAttributes) continue;//only consider "empty" elements with attributes!
						els.Add(Tuple.Create((XElement)XNode.ReadFrom(reader), songSource.Position));
					}
				}

			}).ContinueWith(_ => els.CompleteAdding());
			int songCount = 0;
			foreach (var xElAndPos in els.GetConsumingEnumerable()) {
				ISongFileData song = null;

				try {
					//string elName = reader.Name;
					//var attrs = new Dictionary<string, string>();
					//while (reader.MoveToNextAttribute()) 
					//    attrs[reader.Name] = reader.Value;
					song = ConstructFromXElement(baseUri, xElAndPos.Item1, songsLocal, popEst);
				} catch (Exception e) {
					Console.WriteLine(e);
				}
				if (song != null) {
					songCount++;
					double ratioDone =
						streamLength == -1 ?
						1 - 10000 / (double)(songCount + 10000) :
						(double)xElAndPos.Item2 / streamLength;
					songSink(song, ratioDone);
				} else {
					Console.WriteLine("???");
				}
			}
		}

		/// <summary>
		/// Parses an M3U file and retrieves all songs.  Note that you must specify the encoding of the stream.
		/// Generally, for .m3u files this is  Encoding.GetEncoding(1252) and for .m3u8 files this is Encoding.UTF8
		/// The Encoding is passed separately from the stream to enable seeking, if possible, which is not possible in
		///  a TextReader based approach.
		/// </summary>
		/// <param name="tr">The stream to read from</param>
		/// <param name="songSink">The handler to call for each found song</param>
		public static void LoadSongsFromM3U(TextReader tr, Action<ISongFileData> songSink, bool? songsLocal) {
			string nextLine = tr.ReadLine();
			bool extm3u = nextLine == "#EXTM3U";
			if (extm3u) nextLine = tr.ReadLine();
			while (nextLine != null) {//read another song!
				string metaLine = null;
				while (nextLine != null && nextLine.StartsWith("#") || nextLine.Trim().Length == 0) {//ignore comments or empty lines, but keep "last" comment line for EXTM3U meta-info.
					metaLine = nextLine;
					nextLine = tr.ReadLine();
				}
				// ReSharper disable HeuristicUnreachableCode
				// ReSharper disable ConditionIsAlwaysTrueOrFalse
				if (nextLine == null) break;
				// ReSharper restore ConditionIsAlwaysTrueOrFalse
				// ReSharper restore HeuristicUnreachableCode

				Uri songUri;
				if (!Uri.TryCreate(nextLine, UriKind.Absolute, out songUri))
					if (!Uri.TryCreate(Path.GetFullPath(nextLine), UriKind.Absolute, out songUri))
						throw new Exception("Can't parse m3u's paths!");


				songSink(extm3u && metaLine != null
						? new PartialSongFileData(null, metaLine, songUri, songsLocal)
						: new MinimalSongFileData((Uri)null, songUri, songsLocal));

				nextLine = tr.ReadLine();
			}
		}

		public static void WriteSongsToM3U(TextWriter writer, IEnumerable<ISongFileData> songs, Func<ISongFileData, string> songToPathMapper = null) {
			songToPathMapper = songToPathMapper ?? (song => (song.SongUri.IsFile ? song.SongUri.LocalPath : song.SongUri.ToString()));
			writer.WriteLine("#EXTM3U");
			foreach (var songdata in songs)
				writer.WriteLine("#EXTINF:" + songdata.Length + "," + songdata.HumanLabel + "\n" + songToPathMapper(songdata));
		}

		public static ISongFileData[] LoadExtM3U(LFile m3ufile) {
			using (var m3uStream = m3ufile.OpenRead())
				return LoadExtM3U(m3uStream, m3ufile.Extension);
		}

		public static ISongFileData[] LoadExtM3U(Stream m3uStream, string extension) {
			using (var reader = new StreamReader(m3uStream, extension.EndsWith("8") ? Encoding.UTF8 : Encoding.GetEncoding(1252))) {
				List<ISongFileData> m3usongs = new List<ISongFileData>();

				LoadSongsFromM3U(reader, m3usongs.Add, null);
				return m3usongs.ToArray();
			}
		}




		public static void LoadSongsFromPathOrUrl(string pathOrUrl, SongDataLoadDelegate songSink, bool? isLocal, string remoteUsername, string remotePass, IPopularityEstimator popEst) {
			Console.WriteLine("Loading songs from " + pathOrUrl + ":");
			Uri uri = new Uri(pathOrUrl);
			string extension = Path.GetExtension(uri.AbsolutePath); ;

			if (uri.IsFile && LFile.ConstructIfExists(uri.LocalPath) != null)
				using (var stream = LFile.OpenRead(pathOrUrl))
					LoadSongsFromStream(stream, extension, songSink, isLocal, popEst);
			else if (uri.IsAbsoluteUri) {
				var req = WebRequest.Create(uri);
				if (remoteUsername != null)
					req.Credentials = new NetworkCredential(remoteUsername, remotePass);
				using (var resp = req.GetResponse())
				using (var stream = resp.GetResponseStream())
					LoadSongsFromStream(stream, extension, songSink, isLocal, popEst);
			} else {
				throw new Exception("Invalid song path: " + pathOrUrl);
			}
			Console.WriteLine("Completed loading songs from " + pathOrUrl);
		}

		public static void LoadSongsFromStream(Stream stream, string extension, SongDataLoadDelegate songSink, bool? isLocal, IPopularityEstimator popEst) {
			if (extension == ".xml")
				LoadSongsFromXmlFrag(null, stream, songSink, isLocal, popEst);
			else if (extension == ".m3u" || extension == ".m3u8")
				using (var reader = new StreamReader(stream, extension == ".m3u8" ? Encoding.UTF8 : Encoding.GetEncoding(1252))) {
					long streamLength = -1;
					try { streamLength = stream.Length; } catch (NotSupportedException) { }
					int songCount = 0;
					LoadSongsFromM3U(reader, song => {
						songCount++;
						double ratioDone = streamLength == -1 ? 1 - 10000 / (double)(songCount + 10000) : (double)stream.Position / (double)streamLength;
						songSink(song, ratioDone);
					}, isLocal);
				}
		}
	}
}
