using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using EmnExtensions;
using EmnExtensions.Filesystem;
using EmnExtensions.Text;

namespace SongDataLib {
	class LocalSongDataConfigSection : AbstractSongDataConfigSection {
		readonly DirectoryInfo localSearchPath;
		readonly Uri localSearchUri;
		public LocalSongDataConfigSection(XElement xEl, SongDataConfigFile dcf)
			: base(xEl, dcf) {
			string searchpath = (string)xEl.Attribute("localPath");
			if (name.IsNullOrEmpty() || searchpath.IsNullOrEmpty()) throw new Exception("Missing attributes for localDB");
			if (!Path.IsPathRooted(searchpath)) throw new Exception("Local search paths must be absolute.");
			localSearchPath = new DirectoryInfo((string)xEl.Attribute("localPath"));
			localSearchUri = new Uri(localSearchPath.FullName, UriKind.Absolute);
		}
		protected override bool IsLocal { get { return true; } }

		public override Uri BaseUri { get { return localSearchUri; } }

		protected override void ScanSongs(FileKnownFilter filter, SongDataLoadDelegate handler, Action<string> errSink) {
			Console.WriteLine("Scanning " + localSearchPath + "...");
			if (!localSearchPath.Exists) throw new DirectoryNotFoundException("Local search path doesn't exist: " + localSearchPath.FullName); //TODO: do this during init instead?
			//string[] newFiles = Directory.GetFiles (localSearchPath.FullName, "*", SearchOption.AllDirectories).Where(s => isExtensionOK(Path.GetExtension(s))).ToArray();
			//var newFiles = localSearchPath.DescendantFiles().ToArray();
			var newFiles = localSearchPath.GetFiles("*", SearchOption.AllDirectories);//.Where(fi => isExtensionOK(fi)).ToArray();


			//Console.WriteLine("{0} potential songs found.", newFiles.Length);
			var i = 0;
			foreach (var newfile in newFiles) {
				i++;
				if (!isExtensionOK(newfile))
					continue;
				Uri songUri = new Uri(newfile.FullName, UriKind.Absolute);
				ISongFileData song = filter(songUri);
				if (song == null || (song is SongFileData && ((SongFileData)song).lastWriteTime < newfile.LastWriteTimeUtc))
					try {
						song = SongFileDataFactory.ConstructFromFile(localSearchUri, newfile, dcf.PopularityEstimator);
					} catch (Exception e) {
						errSink("Non-fatal error while generating XML of file: " + songUri + "\nException:\n" + e); song = null;
					}
				if (song != null) {
					handler(song, (double)i / (double)newFiles.Length);
				}
			}
		}
	}
}
