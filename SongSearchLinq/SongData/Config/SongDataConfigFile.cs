using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using EmnExtensions;

namespace SongDataLib {
	public delegate ISongFileData FileKnownFilter(Uri localSongPath);
	public class SongDataConfigFile : ISongDataConfigSection {
		FileInfo configFile;
		internal DirectoryInfo dataDirectory;
		List<LocalSongDataConfigSection> locals = new List<LocalSongDataConfigSection>();
		List<RemoteSongDataConfigSection> remotes = null;
		const string defaultConfigFileName = "SongSearch.config";
		const string defaultConfigDir = "SongSearch";
		/// <summary>
		/// Load the default config file, picking the first config from the following possibilities:
		/// - ApplicationData (per-user)
		/// - CommonApplicationData (windows: "All Users\Application Data", unix: "/usr/share")
		/// </summary>
		public SongDataConfigFile(bool allowRemote, IPopularityEstimator popEst = null) {
			string configRel = Path.DirectorySeparatorChar.ToString() + defaultConfigDir + Path.DirectorySeparatorChar + defaultConfigFileName;
			string userPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + configRel;
			string globalPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + configRel;
			if (File.Exists(userPath))
				configFile = new FileInfo(userPath);
			else if (File.Exists(globalPath))
				configFile = new FileInfo(globalPath);
			else
				throw new FileNotFoundException("Could not find config file, looked in '" + userPath + "' and '" + globalPath + "'.");
			Init(allowRemote, popEst);
		}
		public DirectoryInfo DataDirectory { get { return dataDirectory; } }
		public SongDataConfigFile(FileInfo configFile, bool allowRemote, IPopularityEstimator popEst = null) {
			if (configFile == null) throw new ArgumentNullException("configFile", "A database config file was not specified");
			if (!configFile.Exists) throw new FileNotFoundException("The specified configuration file wasn't found", configFile.FullName);
			this.configFile = configFile;
			Init(allowRemote, popEst);
		}
		void Init(bool allowRemote, IPopularityEstimator popEst) {
			PopularityEstimator = popEst ?? new NullPopularityEstimator();
			if (allowRemote) remotes = new List<RemoteSongDataConfigSection>();
			Console.WriteLine("Loading config file from " + configFile.FullName);
			using (Stream stream = configFile.OpenRead())
				try {
					XDocument doc = XDocument.Load(XmlReader.Create(stream));

					XElement xRoot = doc.Root;
					if (xRoot.Name != "SongDataConfig") throw new SongDataConfigException(this, "Invalid Root Element Name " + ((xRoot.Name.ToStringOrNull()) ?? "?"));
					if ((string)xRoot.Attribute("version") != "1.0") throw new SongDataConfigException(this, "Invalid Config Version " + (((string)xRoot.Attribute("version")) ?? "?"));

					string dataDirAttr = (string)xRoot.Element("general").Attribute("dataDirectory");
					dataDirectory = new DirectoryInfo(Path.Combine(configFile.Directory.FullName + Path.DirectorySeparatorChar, dataDirAttr));
					if (!dataDirectory.Exists) {

						dataDirectory.Create();
					}
					HashSet<string> names = new HashSet<string>();
					foreach (XElement xe in xRoot.Elements()) {
						string dbType = xe.Name.ToStringOrNull();
						switch (dbType) {
							case "localDB":
								locals.Add(new LocalSongDataConfigSection(xe, this));
								if (!names.Add(locals[locals.Count - 1].name))
									throw new SongDataConfigException(this, "Cannot have multiple DB's identically named '" + locals[locals.Count - 1].name + "'.");
								break;
							case "remoteDB":
								if (remotes == null) break;
								remotes.Add(new RemoteSongDataConfigSection(xe, this));
								if (!names.Add(remotes[remotes.Count - 1].name))
									throw new SongDataConfigException(this, "Cannot have multiple DB's identically named '" + remotes[remotes.Count - 1].name + "'.");

								break;
							case "general":
								break;
							default:
								throw new SongDataConfigException(this, "Unknown db type: " + dbType);
						}
					}
				} catch (SongDataConfigException) { throw; } catch (Exception e) { throw new SongDataConfigException(this, e); }
		}

		public void Load(SongDataLoadDelegate handler) {
			foreach (var local in locals) local.Load(handler);
			if (remotes != null) foreach (var remote in remotes) remote.Load(handler);
		}

		public void RescanAndSave(FileKnownFilter filter, SongDataLoadDelegate handler) {//TODO: add logic to prevent rescanning remote dirs too frequently.
			foreach (var local in locals) local.RescanAndSave(filter, handler);
			if (remotes != null) foreach (var remote in remotes) remote.RescanAndSave(filter, handler);
		}
		internal string configPathReadable { get { return configFile == null ? "<null>" : configFile.FullName; } }
		public FileInfo ConfigFile { get { return configFile; } }
		public IPopularityEstimator PopularityEstimator { get; set; }
	}
}
