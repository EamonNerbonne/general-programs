using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using EmnExtensions.MathHelpers;
using SongDataLib;

namespace LastFMspider {
	public class SongTools : IDisposable {
		readonly SongDataConfigFile configFile;

		public SongTools(SongDataConfigFile configFile = null) { this.configFile = configFile ?? new SongDataConfigFile(true); }
		public SongDataConfigFile ConfigFile { get { return configFile; } }

		SongFiles m_SongsOnDisk;
		public SongFiles SongsOnDisk { get { return m_SongsOnDisk ?? (m_SongsOnDisk = new SongFiles(ConfigFile, null)); } }
		public void UnloadDB() { UnloadLookup(); m_SongsOnDisk = null; }

		Dictionary<string, SongFileData> m_FindByPath;
		public Dictionary<string, SongFileData> FindByPath { get { return m_FindByPath ?? (m_FindByPath = SongsOnDisk.Songs.ToDictionary(song => song.SongUri.ToString())); } }
		ILookup<SongRef, SongFileData> m_FindByName;
		public ILookup<SongRef, SongFileData> FindByName { get { return m_FindByName ?? (m_FindByName = SongsOnDisk.Songs.ToLookup(SongRef.Create)); } }
		public void UnloadLookup() { m_FindByPath = null; m_FindByPath = null; }

		SongSimilarityCache similarSongs;
		public SongSimilarityCache SimilarSongs { get { return similarSongs ?? (similarSongs = new SongSimilarityCache(this)); } }

		LastFMSQLiteCache m_LastFmCache;
		public LastFMSQLiteCache LastFmCache { get { return m_LastFmCache ?? (m_LastFmCache = new LastFMSQLiteCache(configFile)); } }


		internal void LogNonFatalError(string message, Exception e) {
			string errstring = "err" + DateTime.UtcNow.Ticks + ".log";
			string fullpath = Path.Combine(ConfigFile.DataDirectory.FullName, errstring);
			int triesToGo = 10;
			while (true)
				try {
					using (var stream = File.Open(fullpath, FileMode.Append))
					using (var writer = new StreamWriter(stream)) {
						writer.WriteLine("\n{1}\nAt date: {0}", DateTime.Now, message ?? "<nullmessage>");
						writer.WriteLine("Error Occured in " + Assembly.GetEntryAssembly().FullName);
						if (e != null) writer.WriteLine(e.ToString());
					}
				} catch (IOException ioe) {
					if (triesToGo > 0) {
						triesToGo--;
						Thread.Sleep((int)RndHelper.MakeSecureUInt() % 100);
					} else throw new Exception("Logging IOException:" + ioe, e);
				}
		}



		public void Dispose() {
			throw new NotImplementedException();
		}
	}
}
