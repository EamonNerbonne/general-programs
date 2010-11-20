using System;
using System.Collections.Generic;
using System.IO;

namespace SongDataLib {
	public class SongsOnDisk {
		public DirectoryInfo DatabaseDirectory { get { return configFile.dataDirectory; } }
		public int InvalidDataCount { get { return ignoreSongCount; } }
		public SongData[] Songs { get { return songs; } }

		readonly SongDatabaseConfigFile configFile;

		readonly SongData[] songs;
		int ignoreSongCount = 0;

		/// <summary>
		/// Loads all songs, or those determined by a filter from a config file and ignores the rest.  
		/// </summary>
		/// <param name="configFile">the config file to load.</param>
		/// <param name="filter">The filter to apply, if null, select all songs.</param>
		public SongsOnDisk(SongDatabaseConfigFile configFile, Func<SongData, bool> filter) {
			this.configFile = configFile;
			List<SongData> songsList = new List<SongData>();
			configFile.Load((newsong, estimatedCompletion) => {
				SongData songdata = newsong as SongData;
				if (songdata != null && (filter == null || filter(songdata)))
					songsList.Add(songdata);
				else ignoreSongCount++;
			});
			songs= songsList.ToArray();
		}
	}
}
