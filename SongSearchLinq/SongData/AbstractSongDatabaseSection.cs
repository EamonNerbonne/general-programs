using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace SongDataLib
{

	abstract class AbstractSongDatabaseSection : ISongDatabaseSection
	{
		protected SongDatabaseConfigFile dcf;
		public string name;
		protected FileInfo dbFile;
		protected abstract bool IsLocal { get; }

		public void Load(SongDataLoadDelegate handler) {
			if(dbFile.Exists)
				using(Stream stream = dbFile.OpenRead())
					SongDataFactory.LoadSongsFromXmlFrag(stream, handler, IsLocal);
		}

		public void RescanAndSave(FileKnownFilter filter, SongDataLoadDelegate handler) {
			FileInfo tmpFile = new FileInfo(dbFile.FullName + ".tmp");
			using(Stream stream = tmpFile.OpenWrite())
			using(StreamWriter writer = new StreamWriter(stream))		{
				writer.WriteLine("<songs>");//we're not using an XmlWriter so that if part of the writer throws an unexpected exception, the writer isn't left in an invalid state.
				ScanSongs(filter, delegate(ISongData song, double ratio) {
					//Console.WriteLine(song.SongPath);
					try {
						writer.WriteLine(song.ConvertToXml(null).ToString());
						handler(song, ratio);
					} catch(Exception e) { Console.WriteLine("Non-fatal error while writing XML of file: " + song.SongPath + "\nException:\n"+ e); }
				});
				writer.WriteLine("</songs>");
			}
			
			
			bool tryagain = false;
			try {
				tmpFile.MoveTo(dbFile.FullName);
				Console.WriteLine("DB is new: moved " + tmpFile + " to " + dbFile);
			} catch(IOException) {
				tryagain = true;
			}
			if(tryagain) {
				if(dbFile.IsReadOnly) dbFile.IsReadOnly = false;
				Console.WriteLine("Replacing old DB and backing it up: " + dbFile);
				File.Delete(dbFile.FullName + ".backup");//note that aFileInfo.MoveTo(newfilepath) actually updates the aFileInfo object!
				File.Move(dbFile.FullName, dbFile.FullName + ".backup");//not using FileInfo.Replace as this is not supported by mono
				File.Move(tmpFile.FullName, dbFile.FullName);
			}
			Console.WriteLine("Scanning of DB " + name + " complete.");
		}
		protected static bool isExtensionOK(FileInfo fi) {
			string extension = fi.Extension.ToLowerInvariant();
			return extension == ".mp3";//TODO: reenable! || extension == ".ogg" || extension == ".mpc" || extension == ".mpp" || extension == ".mp+" || extension == ".wma";
		}

		protected abstract void ScanSongs(FileKnownFilter filter, SongDataLoadDelegate handler);

		protected AbstractSongDatabaseSection(XElement xEl, SongDatabaseConfigFile dcf) {
			this.dcf = dcf;
			name = (string)xEl.Attribute("name");
			dbFile = new FileInfo(Path.Combine(dcf.dataDirectory.FullName + Path.DirectorySeparatorChar, name + ".xml"));

		}

	}

}
