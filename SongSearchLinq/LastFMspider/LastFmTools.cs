using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using System.IO;
using EmnExtensions.Algorithms;
using EmnExtensions;
using LastFMspider.LastFMSQLiteBackend;
using LastFMspider.OldApi;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using EmnExtensions.MathHelpers;
using System.Reflection;

namespace LastFMspider {
	public class LastFmTools {
		SongSimilarityCache similarSongs;
		SongDatabaseConfigFile configFile;
		SimpleSongDB db;
		SongDataLookups lookup;
		//  private object sync = new object();

		public SongSimilarityCache SimilarSongs {
			get {
				// lock (sync)
				return similarSongs ?? (similarSongs = new SongSimilarityCache(ConfigFile));
			}
		}
		public SongDatabaseConfigFile ConfigFile { get { return configFile; } }
		public SimpleSongDB DB {
			get {
				// lock (sync)
				return db ?? (db = new SimpleSongDB(ConfigFile, null));
			}
		}

		public SongDataLookups Lookup {
			get {
				//  lock (sync)
				return lookup ?? (lookup = new SongDataLookups(DB.Songs, null));
			}
		}

		public LastFmTools(SongDatabaseConfigFile configFile = null) {
			this.configFile = configFile ?? new SongDatabaseConfigFile(true);
		}


		public void UnloadLookup() {
			// lock (sync)
			lookup = null;
		}

		public void UnloadDB() {
			//  lock (sync) {
			UnloadLookup();
			db = null;
			// }
		}

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
					} else throw new Exception("Logging IOException:"+ioe,e);
				}
		}
	}
}
