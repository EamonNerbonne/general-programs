using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using EmnExtensions.IO;
using EmnExtensions.Text;

namespace SongDataLib {
	class LocalSongDataConfigSection : AbstractSongDataConfigSection {
		readonly LDirectory localSearchPath;
		readonly Uri localSearchUri;
		public LocalSongDataConfigSection(XElement xEl, SongDataConfigFile dcf)
			: base(xEl, dcf) {
			string searchpath = (string)xEl.Attribute("localPath");
			if (name.IsNullOrEmpty() || searchpath.IsNullOrEmpty()) throw new Exception("Missing attributes for localDB");
			if (!Path.IsPathRooted(searchpath)) throw new Exception("Local search paths must be absolute.");
			localSearchPath = new LDirectory((string)xEl.Attribute("localPath"));
			localSearchUri = new Uri(localSearchPath.FullName, UriKind.Absolute);
		}
		protected override bool IsLocal { get { return true; } }

		public override Uri BaseUri { get { return localSearchUri; } }

		protected override void ScanSongs(FileKnownFilter filter, SongDataLoadDelegate handler, Action<string, Exception> errSink) {
			Console.WriteLine("Scanning " + localSearchPath.FullName + "...");
			if (!localSearchPath.Exists) throw new DirectoryNotFoundException("Local search path doesn't exist: " + localSearchPath.FullName); //TODO: do this during init instead?
			var newFiles = localSearchPath.GetFiles("*", SearchOption.AllDirectories).ToArray();//.Where(fi => isExtensionOK(fi)).ToArray();


			//Console.WriteLine("{0} potential songs found.", newFiles.Length);
			var i = 0;
			foreach (var newfile in newFiles) {
				i++;
				try {
					if (!isExtensionOK(newfile))
						continue;
					Uri songUri = new Uri(newfile.FullName, UriKind.Absolute);
					ISongFileData song = filter(songUri);
					if (song == null || (song is SongFileData && ((SongFileData)song).LastWriteTimeUtc < newfile.LastWriteTimeUtc)) {
						try {
							song = SongFileDataFactory.ConstructFromFile(localSearchUri, newfile, dcf.PopularityEstimator);
						} catch (Exception e) {
							errSink("Cannot scan audio file (corrupt file will be skipped): " + songUri, e); song = null;
						}
					} else if (song is SongFileData && ((SongFileData)song).LastWriteTimeUtc < DateTime.UtcNow - TimeSpan.FromDays(90)) {
						var songfile = (SongFileData)song;
						songfile.popularity = dcf.PopularityEstimator.EstimatePopularity(songfile.artist, songfile.title);
					}
					//else if (song is SongFileData) {
					//    var songF = ((SongFileData)song);
					//    songF.popularity = dcf.PopularityEstimator.EstimatePopularity(songF.artist, songF.title);
					//}
					if (song != null) {
						handler(song, (double)i / newFiles.Length);
					}
				} catch (Exception e) {
					errSink("Cannot process file (unreadable file will be skipped): " + newfile.FullName, e);
				}
			}
		}
	}
}
