using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using EamonExtensionsLinq.Text;
using EamonExtensionsLinq;
using EamonExtensionsLinq.Filesystem;

namespace SongDataLib
{
	public delegate bool DatabaseUpdateHandler(string songpath);
	public class DatabaseConfigFile
	{
		FileInfo configFile;
		internal DirectoryInfo dataDirectory;
		List<LocalDatabaseEntry> locals = new List<LocalDatabaseEntry>();
		List<M3UBasedDatabase> remotes = new List<M3UBasedDatabase>();

		public DatabaseConfigFile(FileInfo configFile)
		{
			if (configFile == null) throw new ArgumentNullException("configFile", "A database config file was not specified");
			if (!configFile.Exists) throw new FileNotFoundException("The specified configuration file wasn't found", configFile.FullName);
			this.configFile = configFile;
			try {
				XDocument doc = XDocument.Load(XmlReader.Create(configFile.OpenRead()));

				XElement xRoot = doc.Root;
				if (xRoot.Name != "SongDataConfig") throw new DatabaseConfigException(this, "Invalid Root Element Name " + ((xRoot.Name.ToStringOrNull()) ?? "?"));
				if ((string)xRoot.Attribute("version") != "1.0") throw new DatabaseConfigException(this, "Invalid Config Version " + (((string)xRoot.Attribute("version")) ?? "?"));

				string dataDirAttr = (string)xRoot.Element("general").Attribute("dataDirectory");
				dataDirectory = new DirectoryInfo(Path.Combine(configFile.Directory.FullName + Path.DirectorySeparatorChar, dataDirAttr));
				if (!dataDirectory.Exists) throw new DatabaseConfigException(this, "data directory doesn't exist: " + (dataDirAttr ?? "<null>"));

				foreach (XElement xe in xRoot.Elements()) {
					string dbType = xe.Name.ToStringOrNull();
					switch (dbType) {
						case "localDB":
							locals.Add(new LocalDatabaseEntry(xe, this));
							break;
						case "remoteDB":
							remotes.Add(new M3UBasedDatabase(xe, this));
							break;
						case "general":
							break;
						default:
							throw new DatabaseConfigException(this, "Unknown db type: " + dbType);
					}
				}
			} catch (DatabaseConfigException) { throw; } catch (Exception e) { throw new DatabaseConfigException(this, e); }
		}

		public void Load(DatabaseUpdateHandler handler)
		{
			foreach (var local in locals) local.Load(handler);
		}

		public void Rescan(DatabaseUpdateHandler handler)
		{
			foreach (var local in locals) local.Rescan(handler);
		}

		public void Save(DatabaseUpdateHandler handler)
		{
			foreach (var local in locals) local.Save(handler);
		}

		public IEnumerable<ISongData> Songs { get { return locals.SelectMany(local => local.Songs); } }
		internal string configPath { get { return configFile == null ? "<null>" : configFile.FullName; } }
	}

	public interface ISongDataCollection
	{
		IEnumerable<ISongData> Songs { get; }
		void Load(DatabaseUpdateHandler handler);
		void Clear();
		void Rescan(DatabaseUpdateHandler handler);
		void Save(DatabaseUpdateHandler handler);
		bool IsLocal { get; }
	}

	class LocalDatabaseEntry : ISongDataCollection
	{
		DatabaseConfigFile dcf;
		public string name;
		FileInfo dbFile;
		DirectoryInfo localSearchPath;
		ISongData[] songs;
		public LocalDatabaseEntry(XElement xEl, DatabaseConfigFile dcf)
		{
			this.dcf = dcf;

			name = (string)xEl.Attribute("name");
			string searchpath = (string)xEl.Attribute("localPath");
			if (name.IsNullOrEmpty() || searchpath.IsNullOrEmpty()) throw new Exception("Missing attributes for localDB");
			if (!Path.IsPathRooted(searchpath)) throw new Exception("Local search paths must be absolute.");
			localSearchPath = new DirectoryInfo((string)xEl.Attribute("localPath"));
			if (!localSearchPath.Exists) throw new DirectoryNotFoundException("Local search path doesn't exist: " + localSearchPath.FullName);
			dbFile = new FileInfo(Path.Combine(dcf.dataDirectory.FullName + Path.DirectorySeparatorChar, name + ".xml"));
			songs = null;
		}

		public void Load(DatabaseUpdateHandler handler)
		{
			if (!dbFile.Exists) { songs = new ISongData[0]; return; }
			XmlReaderSettings settings = new XmlReaderSettings();
			settings.ConformanceLevel = ConformanceLevel.Fragment;
			List<ISongData> songlist = new List<ISongData>();
			TextReader textreader = dbFile.OpenText();
			try {
				XmlReader reader = XmlReader.Create(textreader, settings);
				while (reader.Read()) {
					if (!reader.IsEmptyElement) continue;
					ISongData song=null;
					try {
						song = SongDataFactory.LoadFromXElement((XElement)XElement.ReadFrom(reader));
					} catch (Exception e) {
						Console.WriteLine(e);
					}
					if (song != null) {
						songlist.Add(song);
						handler(song.SongPath);
						Console.WriteLine(((SongData)song).lastWriteTime);
					} else {
						Console.WriteLine("???");
					}
				}
				reader.Close();
			} finally {
				textreader.Close();
			}
			songs = songlist.Where(w => w != null).ToArray();
		}

		public IEnumerable<ISongData> Songs
		{
			get { return songs; }
		}

		public void Rescan(DatabaseUpdateHandler handler)
		{//TODO:do this in parallel, I mean, why not?
			Dictionary<string, ISongData> songsByPath = new Dictionary<string, ISongData>();
			List<ISongData> newsongs = new List<ISongData>();

			if (songs != null)
				foreach (ISongData song in songs)
					songsByPath[song.SongPath] = song;


			foreach (FileInfo newfile in localSearchPath.DescendantFiles().Where(fi => isExtensionOK(fi))) {
				if (songsByPath.ContainsKey(newfile.FullName)) {
					SongData oldSong = songsByPath[newfile.FullName] as SongData;
					if (oldSong != null) {
						if (oldSong.lastWriteTime == newfile.LastWriteTime) {
							newsongs.Add(oldSong);
							handler(null);
							continue;
						} else {
							Console.WriteLine("Hmm: A: " + oldSong.lastWriteTime + "  (" + oldSong.lastWriteTime.Ticks+")");
							Console.WriteLine("Hmm: A: " + newfile.LastWriteTime + "  (" + newfile.LastWriteTime.Ticks + ")");
						}
					}
				}
				ISongData song = FuncUtil.Swallow(() => SongDataFactory.LoadFromFile(newfile), () => null);
				if (song != null) {
					newsongs.Add(song);
					handler(song.SongPath);
				}
			}
			songs = newsongs.ToArray();
		}

		static bool isExtensionOK(FileInfo fi)
		{
			string extension = fi.Extension.ToLower();
			return extension == ".mp3" || extension == ".ogg" || extension == ".mpc" || extension == ".mpp" || extension == ".mp+" || extension == ".wma";
		}

		public bool IsLocal { get { return true; } }

		public void Clear()
		{
			songs = null;
		}

		public void Save(DatabaseUpdateHandler handler)
		{
			FileInfo tmpFile = new FileInfo(dbFile.FullName + ".tmp");
			try {
				var outputlog = new StreamWriter(tmpFile.OpenWrite());
				DateTime prev = DateTime.Now;
				foreach (ISongData songdata in songs) {
					XElement song = songdata.ConvertToXml();
					outputlog.WriteLine(song.ToString());
					handler(songdata.SongPath);
				}
				outputlog.Flush();
				outputlog.Close();
				dbFile.Delete();
				tmpFile.MoveTo(dbFile.FullName);
			} finally {
				//if(tmpFile.Exists) tmpFile.Delete();
			}
		}
	}

	class M3UBasedDatabase : ISongDataCollection
	{
		DatabaseConfigFile dcf;
		public string name, href, login, pass;
		FileInfo dbFile;
		ISongData[] songs;
		public M3UBasedDatabase(XElement xEl, DatabaseConfigFile dcf)
		{
			this.dcf = dcf;
			name = (string)xEl.Attribute("name");
			href = (string)xEl.Attribute("href");
			login = (string)xEl.Attribute("login");
			pass = (string)xEl.Attribute("pass");
			if (name.IsNullOrEmpty() || href.IsNullOrEmpty()) throw new Exception("Missing attributes for remoteDB");
			dbFile = new FileInfo(Path.Combine(dcf.dataDirectory.FullName + Path.DirectorySeparatorChar, name + ".m3u"));
			songs = null;
		}

		public bool IsLocal { get { return false; } }

		public IEnumerable<ISongData> Songs
		{
			get { return songs; }
		}

		public void Rescan(DatabaseUpdateHandler handler)
		{
			return;//TODO: implement.
		}

		public void Load(DatabaseUpdateHandler handler)
		{
			TextReader tr;
			FileInfo fi = dbFile;
			List<ISongData> songlist = new List<ISongData>();

			if (fi.Extension == ".m3u") tr = new StreamReader(fi.OpenRead(), Encoding.GetEncoding(1252));//open as normal M3U with codepage 1252, and not UTF-8
			else if (fi.Extension == ".m3u8") tr = fi.OpenText();//open as UTF-8
			else throw new ArgumentException("Don't know how to deal with file " + fi.FullName);
			string nextLine = tr.ReadLine();
			bool extm3u = nextLine == "#EXTM3U";
			if (extm3u) nextLine = tr.ReadLine();
			while (nextLine != null) {//read another song!
				ISongData song;
				if (extm3u) {
					string uri = tr.ReadLine();
					if (uri == null) break;//invalid M3U, though
					song = new PartialSongData(nextLine, uri);
				} else {
					song = new MinimalSongData(nextLine);
				}
				songlist.Add(song);
				handler(song.SongPath);
				nextLine = tr.ReadLine();
			}

		}

		public void Clear()
		{
			return;//TODO: implement.
		}

		public void Save(DatabaseUpdateHandler handler)
		{
			return;//TODO: implement.
		}
	}

	public class DatabaseConfigException : Exception
	{
		public DatabaseConfigException(DatabaseConfigFile databaseConfigFile, string message) : base("Error while parsing " + databaseConfigFile.configPath + ":\n" + message) { }
		public DatabaseConfigException(DatabaseConfigFile databaseConfigFile, Exception innerException) : base("Error while parsing " + databaseConfigFile.configPath + ".", innerException) { }
	}
}
