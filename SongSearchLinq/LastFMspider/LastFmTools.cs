using System;
using System.IO;
using System.Reflection;
using System.Threading;
using EmnExtensions.MathHelpers;
using SongDataLib;
using System.Collections.Generic;
using System.Linq;

namespace LastFMspider {
	public class LastFmTools {
		SongSimilarityCache similarSongs;
		readonly SongDataConfigFile configFile;
		SongFiles db;

		public SongSimilarityCache SimilarSongs { get { return similarSongs ?? (similarSongs = new SongSimilarityCache(ConfigFile)); } }
		public SongDataConfigFile ConfigFile { get { return configFile; } }
		public SongFiles SongsOnDisk { get { return db ?? (db = new SongFiles(ConfigFile, null)); } }


		Dictionary<string, SongFileData> m_FindByPath;
		public Dictionary<string, SongFileData> FindByPath { get { return m_FindByPath ?? (m_FindByPath = SongsOnDisk.Songs.ToDictionary(song => song.SongUri.ToString())); } }

		ILookup<SongRef, SongFileData> m_FindByName;
		public ILookup<SongRef, SongFileData> FindByName { get { return m_FindByName ?? (m_FindByName = SongsOnDisk.Songs.ToLookup(SongRef.Create)); } }


		public LastFmTools(SongDataConfigFile configFile = null) {
			this.configFile = configFile ?? new SongDataConfigFile(true);
		}


		public void UnloadLookup() { m_FindByPath = null; m_FindByPath =null; }

		public void UnloadDB() { UnloadLookup(); db = null; }

		/// <summary>
		/// Downloads Last.fm metadata for all tracks in the song database (if not already present).
		/// </summary>
		/// <param name="shuffle">Whether to perform the precaching in a random order.  Doing so slows down the precaching when almost all
		/// items are already downloaded, but permits multiple download threads to run in parallel without duplicating downloads.</param>
		public void PrecacheLocalFiles(bool shuffle = false) { ToolsInternal.PrecacheLocalFiles(this, shuffle); }

		public void EnsureLocalFilesInDB() { ToolsInternal.EnsureLocalFilesInDB(this); }

		public int PrecacheSongSimilarity() { return ToolsInternal.PrecacheSongSimilarity(this); }

		public int PrecacheArtistSimilarity() { return ToolsInternal.PrecacheArtistSimilarity(this); }

		public int PrecacheArtistTopTracks() { return ToolsInternal.PrecacheArtistTopTracks(this); }

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
	}
}
