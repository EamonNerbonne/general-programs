using System;
using System.IO;
using System.Reflection;
using System.Threading;
using EmnExtensions.MathHelpers;
using SongDataLib;

namespace LastFMspider {
	public class LastFmTools {
		SongSimilarityCache similarSongs;
		readonly SongDatabaseConfigFile configFile;
		SongsOnDisk db;
		SongDataLookups lookup;

		public SongSimilarityCache SimilarSongs { get { return similarSongs ?? (similarSongs = new SongSimilarityCache(ConfigFile)); } }
		public SongDatabaseConfigFile ConfigFile { get { return configFile; } }
		public SongsOnDisk SongsOnDisk { get { return db ?? (db = new SongsOnDisk(ConfigFile, null)); } }

		public SongDataLookups Lookup { get { return lookup ?? (lookup = new SongDataLookups(SongsOnDisk.Songs, null)); } }

		public LastFmTools(SongDatabaseConfigFile configFile = null) {
			this.configFile = configFile ?? new SongDatabaseConfigFile(true);
		}


		public void UnloadLookup() { lookup = null; }

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
