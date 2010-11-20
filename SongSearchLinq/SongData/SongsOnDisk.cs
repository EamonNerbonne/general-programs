using System;
using System.Collections.Generic;
using System.IO;

namespace SongDataLib {
	public class SongsOnDisk {
		public DirectoryInfo DatabaseDirectory { get { return configFile.dataDirectory; } }
		public int InvalidDataCount { get { return ignoreSongCount; } }
		public SongFileData[] Songs { get { return songs; } }

		readonly SongDataConfigFile configFile;

		readonly SongFileData[] songs;
		int ignoreSongCount = 0;

		/// <summary>
		/// Loads all songs, or those determined by a filter from a config file and ignores the rest.  
		/// </summary>
		/// <param name="configFile">the config file to load.</param>
		/// <param name="filter">The filter to apply, if null, select all songs.</param>
		public SongsOnDisk(SongDataConfigFile configFile, Func<SongFileData, bool> filter) {
			this.configFile = configFile;
			List<SongFileData> songsList = new List<SongFileData>();
			configFile.Load((newsong, estimatedCompletion) => {
				SongFileData songdata = newsong as SongFileData;
				if (songdata != null && (filter == null || filter(songdata)))
					songsList.Add(songdata);
				else ignoreSongCount++;
			});
			songs= songsList.ToArray();
		}
	}
}
