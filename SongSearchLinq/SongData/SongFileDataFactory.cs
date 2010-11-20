using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using EmnExtensions;

namespace SongDataLib {
	public delegate void SongDataLoadDelegate(ISongFileData newsong, double estimatedCompletion);
	public static class SongFileDataFactory {
		public static ISongFileData ConstructFromFile(FileInfo fileObj, IPopularityEstimator popEst) { return new SongFileData(fileObj,popEst); }
		public static ISongFileData ConstructFromXElement(XElement xEl, bool? isLocal,IPopularityEstimator popEst) {
			if (xEl.Name == "song") {
				return new SongFileData(xEl, isLocal,popEst);
			}
			else if (xEl.Name == "partsong") {
				return new PartialSongData(xEl, isLocal);
			}
			else if (xEl.Name == "songref") {
				return new MinimalSongFileData(xEl, isLocal);
			}
			else
				throw new ArgumentException("Don't recognize xml name " + xEl.Name + ", is not a valid ISongData format.", "xEl");
		}

		public static void LoadSongsFromXmlFrag(Stream songSource, SongDataLoadDelegate songSink, bool? songsLocal, IPopularityEstimator popEst) {
			//XmlReaderSettings settings = new XmlReaderSettings();
			//settings.ConformanceLevel = ConformanceLevel.Fragment;
			long streamLength = F.Swallow(() => songSource.Length, () => -1);
			using (var textreader = new StreamReader(songSource))
			using (var reader = XmlReader.Create(textreader)) {
				int songCount = 0;
				while (reader.Read()) {
					if (!reader.IsEmptyElement || !reader.HasAttributes) continue;//only consider "empty" elements with attributes!
					ISongFileData song = null;
					try {
						//string elName = reader.Name;
						//var attrs = new Dictionary<string, string>();
						//while (reader.MoveToNextAttribute()) 
						//    attrs[reader.Name] = reader.Value;
						song = SongFileDataFactory.ConstructFromXElement((XElement)XElement.ReadFrom(reader), songsLocal, popEst);
					}
					catch (Exception e) {
						Console.WriteLine(e);
					}
					if (song != null) {
						songCount++;
						double ratioDone =
							streamLength == -1 ?
							1 - 10000 / (double)(songCount + 10000) :
							(double)songSource.Position / (double)streamLength;
						songSink(song, ratioDone);
					}
					else {
						Console.WriteLine("???");
					}
				}
			}
		}

		/// <summary>
		/// Parses an M3U file and retrieves all songs.  Note that you must specify the encoding of the stream.
		/// Generally, for .m3u files this is  Encoding.GetEncoding(1252) and for .m3u8 files this is Encoding.UTF8
		/// The Encoding is passed separately from the stream to enable seeking, if possible, which is not possible in
		///  a TextReader based approach.
		/// </summary>
		/// <param name="songSource">The stream to read from</param>
		/// <param name="songSink">The handler to call for each found song</param>
		/// <param name="encoding">The encoding to use for decoding the stream.</param>
		public static void LoadSongsFromM3U(Stream songSource, SongDataLoadDelegate songSink, Encoding encoding, bool? songsLocal) {
			long streamLength = F.Swallow(() => songSource.Length, () => -1);
			int songCount = 0; //fallback guesstimation
			StreamReader tr;
			tr = new StreamReader(songSource, encoding);
			string nextLine = tr.ReadLine();
			bool extm3u = nextLine == "#EXTM3U";
			if (extm3u) nextLine = tr.ReadLine();
			while (nextLine != null) {//read another song!
				ISongFileData song;
				string metaLine = null;
				while (nextLine != null && nextLine.StartsWith("#") || nextLine.Trim().Length == 0) {//ignore comments or empty lines, but keep "last" comment line for EXTM3U meta-info.
					metaLine = nextLine;
					nextLine = tr.ReadLine();
				}
				if (nextLine == null) break;

				Uri songUri;
				if (!Uri.TryCreate(nextLine, UriKind.Absolute, out songUri))
					if (!Uri.TryCreate(Path.GetFullPath(nextLine), UriKind.Absolute, out songUri))
						throw new Exception("Can't parse m3u's paths!");


				if (extm3u && metaLine != null) {
					song = new PartialSongData(metaLine, songUri, songsLocal);
				}
				else {
					song = new MinimalSongFileData(songUri, songsLocal);
				}
				songCount++;
				double ratioDone =
					streamLength == -1 ?
					1 - 10000 / (double)(songCount + 10000) :
					(double)songSource.Position / (double)streamLength;
				songSink(song, ratioDone);

				nextLine = tr.ReadLine();
			}

		}

		public static void LoadSongsFromPathOrUrl(string pathOrUrl, SongDataLoadDelegate songSink, bool? isLocal, string remoteUsername, string remotePass,IPopularityEstimator popEst) {
			Console.WriteLine("Loading songs from " + pathOrUrl + ":");
			Uri uri = new Uri(pathOrUrl);
			string extension = Path.GetExtension(uri.AbsolutePath); ;

			if (uri.IsFile && File.Exists(uri.LocalPath))
				using (var stream = File.OpenRead(pathOrUrl))
					LoadSongsFromStream(stream, extension, songSink, isLocal,popEst);
			else if (uri.IsAbsoluteUri) {
				var req = WebRequest.Create(uri);
				if (remoteUsername != null)
					req.Credentials = new NetworkCredential(remoteUsername, remotePass);
				using (var resp = req.GetResponse())
				using (var stream = resp.GetResponseStream())
					LoadSongsFromStream(stream, extension, songSink, isLocal,popEst);
			}
			else {
				throw new Exception("Invalid song path: " + pathOrUrl);
			}
			Console.WriteLine("Completed loading songs from " + pathOrUrl);
		}
		public static void LoadSongsFromStream(Stream stream, string extension, SongDataLoadDelegate songSink, bool? isLocal,IPopularityEstimator popEst) {
			if (extension == ".xml")
				LoadSongsFromXmlFrag(stream, songSink, isLocal,popEst);
			else if (extension == ".m3u")
				LoadSongsFromM3U(stream, songSink, Encoding.GetEncoding(1252), isLocal);
			else if (extension == ".m3u8")
				LoadSongsFromM3U(stream, songSink, Encoding.UTF8, isLocal);
		}


	}
}
