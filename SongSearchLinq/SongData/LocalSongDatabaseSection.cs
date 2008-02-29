using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using EamonExtensionsLinq;
using EamonExtensionsLinq.Filesystem;
using EamonExtensionsLinq.Text;

namespace SongDataLib
{
	class LocalSongDatabaseSection : AbstractSongDatabaseSection
	{
		DirectoryInfo localSearchPath;
		public LocalSongDatabaseSection(XElement xEl, SongDatabaseConfigFile dcf)
			: base(xEl, dcf) {
			string searchpath = (string)xEl.Attribute("localPath");
			if(name.IsNullOrEmpty() || searchpath.IsNullOrEmpty()) throw new Exception("Missing attributes for localDB");
			if(!Path.IsPathRooted(searchpath)) throw new Exception("Local search paths must be absolute.");
			localSearchPath = new DirectoryInfo((string)xEl.Attribute("localPath"));
			if(!localSearchPath.Exists) throw new DirectoryNotFoundException("Local search path doesn't exist: " + localSearchPath.FullName);
		}
		protected override bool IsLocal { get { return true; } }

		protected override void ScanSongs(FileKnownFilter filter, SongDataLoadDelegate handler) {
			Console.WriteLine("Scanning " + localSearchPath + "...");
			FileInfo[] newFiles = localSearchPath.DescendantFiles().Where(fi => isExtensionOK(fi)).ToArray();
			Console.WriteLine("{0} potential songs found.", newFiles.Length);
			for(int i = 0; i < newFiles.Length; i++) {
				FileInfo newfile = newFiles[i];
				ISongData song = filter(newfile.FullName);
				if(song == null || (song is SongData && ((SongData)song).lastWriteTime < newfile.LastWriteTime))
					song = FuncUtil.Swallow(() => SongDataFactory.ConstructFromFile(newfile), () => null);
				if(song != null) {
					handler(song, (double)i / (double)newFiles.Length);
				}
			}
		}
	}
}
