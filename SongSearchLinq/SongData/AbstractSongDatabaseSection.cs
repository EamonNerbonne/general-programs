using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Text;

namespace SongDataLib
{

	abstract class AbstractSongDatabaseSection : ISongDatabaseSection
	{
		protected SongDatabaseConfigFile dcf;
		public string name;
		protected FileInfo dbFile;
		protected abstract bool IsLocal { get; }

		public void Load(SongDataLoadDelegate handler) {
			if (dbFile.Exists)
				using (Stream stream = dbFile.OpenRead())
					SongDataFactory.LoadSongsFromXmlFrag(stream, handler, IsLocal);
		}
		static XmlWriterSettings settings = new XmlWriterSettings {
			 CheckCharacters = false,
			  Indent= true,
			    //Encoding = Encoding.UTF8,
				 
		};

		public void RescanAndSave(FileKnownFilter filter, SongDataLoadDelegate handler) {
			FileInfo tmpFile = new FileInfo(dbFile.FullName + ".tmp");
			FileInfo errFile = new FileInfo(dbFile.FullName + ".err");
			using (Stream stream = tmpFile.Open(FileMode.Create))
			using (StreamWriter writer = new StreamWriter(stream))
			using (Stream streamErr = errFile.Open(FileMode.Create))
			using (StreamWriter writerErr = new StreamWriter(streamErr))
			using (XmlWriter xw = XmlWriter.Create(writer, settings))
			{
				List<XElement> els = new List<XElement>();
				//writer.WriteLine("<songs>");//we're not using an XmlWriter so that if part of the writer throws an unexpected exception, the writer isn't left in an invalid state.
				ScanSongs(filter, delegate(ISongData song, double ratio) {
					els.Add(song.ConvertToXml(null));
					handler(song, ratio);
				}, err => {
					Console.WriteLine(err);
					writerErr.WriteLine(err);
				}
				);
				//writer.WriteLine("</songs>");
				new XElement("songs", els).WriteTo(xw);
			}


			bool tryagain = false;
			try {
				tmpFile.MoveTo(dbFile.FullName);
				Console.WriteLine("DB is new: moved " + tmpFile + " to " + dbFile);
			} catch (IOException) {
				tryagain = true;
			}
			if (tryagain) {
				if (dbFile.IsReadOnly) dbFile.IsReadOnly = false;
				Console.WriteLine("Replacing old DB and backing it up: " + dbFile);
				File.Delete(dbFile.FullName + ".backup");//note that aFileInfo.MoveTo(newfilepath) actually updates the aFileInfo object!
				File.Move(dbFile.FullName, dbFile.FullName + ".backup");//not using FileInfo.Replace as this is not supported by mono
				File.Move(tmpFile.FullName, dbFile.FullName);
			}
			Console.WriteLine("Scanning of DB " + name + " complete.");
		}
		protected static bool isExtensionOK(FileInfo fi) {
			return isExtensionOK(fi.Extension);
		}

		protected static bool isExtensionOK(string extension) {
			extension = extension.ToLowerInvariant();
			return extension == ".mp3"
				|| extension == ".ogg"
				|| extension == ".mpc"
				|| extension == ".mpp"
				|| extension == ".mp+"
				|| extension == ".wma"
				|| extension == "._@mp3";
		}

		protected abstract void ScanSongs(FileKnownFilter filter, SongDataLoadDelegate handler, Action<string> errSink);

		protected AbstractSongDatabaseSection(XElement xEl, SongDatabaseConfigFile dcf) {
			this.dcf = dcf;
			name = (string)xEl.Attribute("name");
			dbFile = new FileInfo(Path.Combine(dcf.dataDirectory.FullName + Path.DirectorySeparatorChar, name + ".xml"));
		}
	}
}
