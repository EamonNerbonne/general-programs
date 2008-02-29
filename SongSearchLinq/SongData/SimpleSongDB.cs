using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SongDataLib
{
	public class SimpleSongDB
	{
		public DirectoryInfo DatabaseDirectory { get { return configFile.dataDirectory; } }
		public int InvalidDataCount { get { return ignoreSongCount; } }
		public List<SongData> Songs { get { return songs; } }

		SongDatabaseConfigFile configFile;

		List<SongData> songs = new List<SongData>();
		int ignoreSongCount = 0;
		void OnSongDataLoad(ISongData newsong, double estimatedCompletion) {
			SongData songdata = newsong as SongData;
			if(songdata != null && (filter==null || filter(songdata)))
				songs.Add(songdata);
			else ignoreSongCount++;
		}

		Func<SongData, bool> filter;

		/// <summary>
		/// Loads all songs, or those determined by a filter from a config file and ignores the rest.  
		/// </summary>
		/// <param name="configFile">the config file to load.</param>
		/// <param name="filter">The filter to apply, if null, select all songs.</param>
		public SimpleSongDB(SongDatabaseConfigFile configFile,Func<SongData,bool> filter) {
			this.filter = filter;
			this.configFile = configFile;
			configFile.Load(OnSongDataLoad);
			songs.Capacity = songs.Count;
		}
	}
}
