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
	}
}
