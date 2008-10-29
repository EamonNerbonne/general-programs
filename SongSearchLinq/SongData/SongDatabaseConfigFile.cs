using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using EmnExtensions;

namespace SongDataLib
{
	public delegate ISongData FileKnownFilter(string songUri);
	public class SongDatabaseConfigFile : ISongDatabaseSection
	{
		FileInfo configFile;
		internal DirectoryInfo dataDirectory;
		List<LocalSongDatabaseSection> locals = new List<LocalSongDatabaseSection>();
		List<RemoteSongDatabaseSection> remotes=null;
		const string defaultConfigFileName = "SongSearch.config";
		const string defaultConfigDir = "SongSearch";
		/// <summary>
		/// Load the default config file, picking the first config from the following possibilities:
		/// - ApplicationData (per-user)
		/// - CommonApplicationData (windows: "All Users\Application Data", unix: "/usr/share")
		/// </summary>
		public SongDatabaseConfigFile(bool allowRemote) {
			string configRel = Path.DirectorySeparatorChar.ToString() + defaultConfigDir + Path.DirectorySeparatorChar + defaultConfigFileName;
			string userPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + configRel;
			string globalPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + configRel;
			if(File.Exists(userPath))
				configFile = new FileInfo(userPath);
			else if(File.Exists(globalPath))
				configFile = new FileInfo(globalPath);
			else
				throw new FileNotFoundException("Could not find config file, looked in '" + userPath + "' and '" + globalPath + "'.");
			Init(allowRemote);
		}
		public DirectoryInfo DataDirectory { get { return dataDirectory; } }
		public SongDatabaseConfigFile(FileInfo configFile,bool allowRemote) {
			if(configFile == null) throw new ArgumentNullException("configFile", "A database config file was not specified");
			if(!configFile.Exists) throw new FileNotFoundException("The specified configuration file wasn't found", configFile.FullName);
			this.configFile = configFile;
			Init(allowRemote);
		}
		void Init(bool allowRemote) {
			if(allowRemote) remotes= new List<RemoteSongDatabaseSection>();
			Console.WriteLine("Loading config file from " + configFile.FullName);
			using(Stream stream = configFile.OpenRead())
				try {
					XDocument doc = XDocument.Load(XmlReader.Create(stream));

					XElement xRoot = doc.Root;
					if(xRoot.Name != "SongDataConfig") throw new SongDatabaseConfigException(this, "Invalid Root Element Name " + ((xRoot.Name.ToStringOrNull()) ?? "?"));
					if((string)xRoot.Attribute("version") != "1.0") throw new SongDatabaseConfigException(this, "Invalid Config Version " + (((string)xRoot.Attribute("version")) ?? "?"));

					string dataDirAttr = (string)xRoot.Element("general").Attribute("dataDirectory");
					dataDirectory = new DirectoryInfo(Path.Combine(configFile.Directory.FullName + Path.DirectorySeparatorChar, dataDirAttr));
					if(!dataDirectory.Exists) {

						dataDirectory.Create();
					}
					HashSet<string> names = new HashSet<string>();
					foreach(XElement xe in xRoot.Elements()) {
						string dbType = xe.Name.ToStringOrNull();
						switch(dbType) {
							case "localDB":
								locals.Add(new LocalSongDatabaseSection(xe, this));
								if(!names.Add(locals[locals.Count - 1].name))
									throw new SongDatabaseConfigException(this, "Cannot have multiple DB's identically named '" + locals[locals.Count - 1].name + "'.");
								break;
							case "remoteDB":
								if(remotes == null) break;
								remotes.Add(new RemoteSongDatabaseSection(xe, this));
								if(!names.Add(remotes[remotes.Count - 1].name))
									throw new SongDatabaseConfigException(this, "Cannot have multiple DB's identically named '" + remotes[remotes.Count - 1].name + "'.");

								break;
							case "general":
								break;
							default:
								throw new SongDatabaseConfigException(this, "Unknown db type: " + dbType);
						}
					}
				} catch(SongDatabaseConfigException) { throw; } catch(Exception e) { throw new SongDatabaseConfigException(this, e); }
		}

		public void Load(SongDataLoadDelegate handler) {
			foreach(var local in locals) local.Load(handler);
			if(remotes!=null) foreach(var remote in remotes) remote.Load(handler);
		}

		public void RescanAndSave(FileKnownFilter filter, SongDataLoadDelegate handler) {//TODO: add logic to prevent rescanning remote dirs too frequently.
			foreach(var local in locals) local.RescanAndSave(filter, handler);
			if(remotes!=null) foreach(var remote in remotes) remote.RescanAndSave(filter, handler);
		}
		internal string configPath { get { return configFile == null ? "<null>" : configFile.FullName; } }
	}


}
