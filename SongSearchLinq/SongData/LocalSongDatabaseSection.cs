using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using EmnExtensions;
using EmnExtensions.Filesystem;
using EmnExtensions.Text;

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
		}
		protected override bool IsLocal { get { return true; } }

		protected override void ScanSongs(FileKnownFilter filter, SongDataLoadDelegate handler) {
			Console.WriteLine("Scanning " + localSearchPath + "...");
            if (!localSearchPath.Exists) throw new DirectoryNotFoundException("Local search path doesn't exist: " + localSearchPath.FullName); //TODO: do this during init instead?
            FileInfo[] newFiles = localSearchPath.DescendantFiles().Where(fi => isExtensionOK(fi)).ToArray();
			Console.WriteLine("{0} potential songs found.", newFiles.Length);

			for(int i = 0; i < newFiles.Length; i++) {
				FileInfo newfile = newFiles[i];
				ISongData song = F.Swallow(()=>filter(newfile.FullName),()=>null);//TODO:need fullname?
				if(song == null || (song is SongData && ((SongData)song).lastWriteTime < newfile.LastWriteTimeUtc))
					song = F.Swallow(() => SongDataFactory.ConstructFromFile(newfile), () => null);
				if(song != null) {
					handler(song, (double)i / (double)newFiles.Length);
				}
			}
		}
	}
}
