using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Text;
using System.Linq;
namespace SongDataLib {

	public abstract class AbstractSongDataConfigSection : ISongDataConfigSection {
		protected readonly SongDataConfigFile dcf;
		public readonly string name;
		protected readonly FileInfo dbFile;
		protected abstract bool IsLocal { get; }

		public abstract Uri BaseUri { get; }

		public void Load(SongDataLoadDelegate handler) {
			if (dbFile.Exists)
				using (Stream stream = dbFile.OpenRead())
					SongFileDataFactory.LoadSongsFromXmlFrag( BaseUri, stream, handler, IsLocal, dcf.PopularityEstimator);
		}
		static readonly XmlWriterSettings settings = new XmlWriterSettings {
			CheckCharacters = false,
			Indent = true,
			//Encoding = Encoding.UTF8,

		};

		public void RescanAndSave(FileKnownFilter filter, SongDataLoadDelegate handler) {
			FileInfo tmpFile = new FileInfo(dbFile.FullName + ".tmp");
			FileInfo errFile = new FileInfo(dbFile.FullName + ".err");
			using (Stream stream = tmpFile.Open(FileMode.Create))
			using (StreamWriter writer = new StreamWriter(stream))
			using (Stream streamErr = errFile.Open(FileMode.Create))
			using (StreamWriter writerErr = new StreamWriter(streamErr))
			using (XmlWriter xw = XmlWriter.Create(writer, settings)) {
				List<ISongFileData> songs = new List<ISongFileData>();
				//writer.WriteLine("<songs>");//we're not using an XmlWriter so that if part of the writer throws an unexpected exception, the writer isn't left in an invalid state.
				ScanSongs(filter, (song, ratio) => {
					//var songdata =song as SongData;
					//if (songdata !=null) 
					//    songdata.popularity = dcf.PopularityEstimator.EstimatePopularity (songdata.artist, songdata.title);

					songs.Add(song);
					handler(song, ratio);
				}, err => {
					Console.WriteLine(err);
					writerErr.WriteLine(err);
				}
				);
				//writer.WriteLine("</songs>");
				new XStreamingElement("songs", songs.Select(song=>song.ConvertToXml(null,false)) ).WriteTo(xw);
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
		
		protected static bool isExtensionOK(FileInfo fi) {return isExtensionOK(fi.Extension);}
		static bool isExtensionOK(string extension) {
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

		protected AbstractSongDataConfigSection(XElement xEl, SongDataConfigFile dcf) {
			this.dcf = dcf;
			name = (string)xEl.Attribute("name");
			dbFile = new FileInfo(Path.Combine(dcf.dataDirectory.FullName + Path.DirectorySeparatorChar, name + ".xml"));
		}
	}
}
