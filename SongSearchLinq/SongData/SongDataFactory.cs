using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using EmnExtensions;
using EmnExtensions.Filesystem;

namespace SongDataLib
{
	public delegate void SongDataLoadDelegate(ISongData newsong, double estimatedCompletion);
	public static class SongDataFactory
	{
		public static ISongData ConstructFromFile(FileInfo fileObj) { return new SongData(fileObj); }
		public static ISongData ConstructFromXElement(XElement xEl, bool? isLocal) {
			if(xEl.Name == "song") {
				return new SongData(xEl, isLocal);
			} else if(xEl.Name == "partsong") {
				return new PartialSongData(xEl, isLocal);
			} else if(xEl.Name == "songref") {
				return new MinimalSongData(xEl, isLocal);
			} else
				throw new ArgumentException("Don't recognize xml name " + xEl.Name + ", is not a valid ISongData format.", "xEl");
		}

		public static void LoadSongsFromXmlFrag(Stream songSource, SongDataLoadDelegate songSink, bool? songsLocal) {
			XmlReaderSettings settings = new XmlReaderSettings();
			settings.ConformanceLevel = ConformanceLevel.Fragment;
			long streamLength = Functional.Swallow(() => songSource.Length, () => -1);
			TextReader textreader = new StreamReader(songSource);
			int songCount = 0;
			try {
				XmlReader reader = XmlReader.Create(textreader, settings);
				while(reader.Read()) {
					if(!reader.IsEmptyElement) continue;//only consider "empty" elements!
					ISongData song = null;
					try {
						song = SongDataFactory.ConstructFromXElement((XElement)XElement.ReadFrom(reader), songsLocal);
					} catch(Exception e) {
						Console.WriteLine(e);
					}
					if(song != null) {
						songCount++;
						double ratioDone =
							streamLength == -1 ?
							1 - 10000 / (double)(songCount + 10000) :
							(double)songSource.Position / (double)streamLength;
						songSink(song, ratioDone);
					} else {
						Console.WriteLine("???");
					}
				}
				reader.Close();
			} finally {
				songSource.Close();
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
			long streamLength = Functional.Swallow(() => songSource.Length, () => -1);
			int songCount = 0; //fallback guesstimation
			StreamReader tr;
			tr = new StreamReader(songSource, encoding);
			string nextLine = tr.ReadLine();
			bool extm3u = nextLine == "#EXTM3U";
			if(extm3u) nextLine = tr.ReadLine();
			while(nextLine != null) {//read another song!
				ISongData song;
				string metaLine = null;
				while(nextLine!=null&&nextLine.StartsWith("#")) {//iignore comments, but keep "last" comment line for EXTM3U meta-info.
					metaLine = nextLine;
					nextLine = tr.ReadLine();
				}
				if(nextLine == null) break;
				if(extm3u && metaLine != null ) {
					song = new PartialSongData(metaLine, nextLine, songsLocal);
				} else {
					song = new MinimalSongData(nextLine, songsLocal);
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

		public static void LoadSongsFromPathOrUrl(string pathOrUrl, SongDataLoadDelegate songSink, bool? isLocal,string remoteUsername,string remotePass) {
			Console.WriteLine("Loading songs from " + pathOrUrl + ":");
			string extension = null;
			Stream stream;
			if(FSUtil.IsValidPath(pathOrUrl) && File.Exists(pathOrUrl)) {
				extension = Path.GetExtension(pathOrUrl).ToLowerInvariant();
				stream = File.OpenRead(pathOrUrl);
			} else if(Uri.IsWellFormedUriString(pathOrUrl, UriKind.Absolute)) {
				Uri uri = new Uri(pathOrUrl);
				extension = Path.GetExtension(uri.AbsolutePath);
				var req = WebRequest.Create(pathOrUrl);
				if(remoteUsername!=null)
					req.Credentials = new NetworkCredential(remoteUsername, remotePass);
				var resp = req.GetResponse();
				stream = resp.GetResponseStream();
			} else throw new Exception("Invalid song path: " + pathOrUrl);

			if(extension == ".xml")
				LoadSongsFromXmlFrag(stream, songSink, isLocal);
			else if(extension == ".m3u")
				LoadSongsFromM3U(stream, songSink, Encoding.GetEncoding(1252), isLocal);
			else if(extension == ".m3u8")
				LoadSongsFromM3U(stream, songSink, Encoding.UTF8, isLocal);
			Console.WriteLine("Completed loading songs from " + pathOrUrl);
		}

	}
}
