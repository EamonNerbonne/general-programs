using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using SongDataLib;
using SuffixTreeLib;
using EmnExtensions.Text;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace SongSearchSite
{

	public sealed class SongDbContainer : IDisposable
	{
		public static string NormalizeSongPath(string localSongPath) {
			StringBuilder sb = new StringBuilder();
			foreach (char c in localSongPath) {
				switch (c) {
					case '\\':
						sb.Append('/');
						break;
					case '%':
					case '*':
					case '&':
					case '+':
					case ':':
					case '!':
						//we filter out %*&: to avoid triggering IIS7 "bad request bug" bug: http://support.microsoft.com/default.aspx?scid=kb;EN-US;826437
						sb.Append('!');
						sb.Append(Convert.ToString((int)c, 16).PadLeft(2, '0'));
						break;
					default:
						if (Canonicalize.FastGetUnicodeCategory(c) == UnicodeCategory.Control)
							goto case '!';//some filenames actually contain control chars and IIS chokes on em.
						else
							sb.Append(c);
						break;
				}
			}
			return sb.ToString();
		}

		public static string NormalizeSongPath(ISongData localSong) {
			if (!localSong.IsLocal)
				throw new ArgumentException("This is only meaningful for local files.");
			return NormalizeSongPath(localSong.SongPath);
		}

		static SongDbContainer Singleton {
			get {
				HttpContext context = HttpContext.Current;
				lock (context.Application) {
					SongDbContainer retval;
					if (context.Application["SongContainer"] == null)
						context.Application["SongContainer"] = retval = new SongDbContainer();
					else
						retval = (SongDbContainer)context.Application["SongContainer"];
					return retval;
				}
			}
		}
		SearchableSongDB searchEngine;
		SongDB db;

		Dictionary<string, ISongData> localSongs = new Dictionary<string, ISongData>();
		object syncroot = new object();
		object initLock = new object();
		FileSystemWatcher fsWatcher;
		public void Dispose() {
			if (fsWatcher != null)
				fsWatcher.Dispose();
		}

		bool isFresh;

		private void Init() {
			lock (initLock) {
				if (isFresh)
					return;
				isFresh = true;
				SongDatabaseConfigFile dcf = new SongDatabaseConfigFile(true);
				List<ISongData> tmpSongs = new List<ISongData>();
				dcf.Load(delegate(ISongData aSong, double ratio) {
					tmpSongs.Add(aSong);
				});
				var new_db = new SongDB(tmpSongs);
				tmpSongs = null;
				var new_localSongs = new_db.songs.Where(s => s.IsLocal).ToDictionary(song => NormalizeSongPath(song));
				var new_searchEngine = new SearchableSongDB(new_db, new SuffixTreeSongSearcher());

				lock (syncroot) {
					db = new_db;
					localSongs = new_localSongs;
					searchEngine = new_searchEngine;
				}

				if (fsWatcher == null) {
					fsWatcher = new FileSystemWatcher {
						Path = dcf.DataDirectory.FullName,
						Filter = "*.xml",
						IncludeSubdirectories = false,
					};
					fsWatcher.Created += (o, e) => { DbUpdated(); };
					fsWatcher.Changed += (o, e) => { DbUpdated(); };
					fsWatcher.Renamed += (o, e) => { DbUpdated(); };
					fsWatcher.Error += (o, e) => { DbUpdated(); };
					fsWatcher.Deleted += (o, e) => { DbUpdated(); };
					fsWatcher.EnableRaisingEvents = true;
				}
			}
		}


		void DbUpdated() {

			isFresh = false;
			ThreadPool.QueueUserWorkItem(o => { 
				Thread.Sleep(5000);
				Init();
			});
		}

		private SongDbContainer() { Init(); }
		public static SearchableSongDB SearchableSongDB { get { var sdc = Singleton; lock (sdc.syncroot) return sdc.searchEngine; } }
		/// <summary>
		/// Determines whether a given path maps to an indexed, local song.  If it doesn't, it returns null.  If it does, it returns the meta data known about the song, including the song's "real" path.
		/// </summary>
		/// <param name="path">The normalized path </param>
		/// <returns></returns>
		public static ISongData GetSongByNormalizedPath(string path) {
			ISongData retval;
			var sdc = Singleton;
			Dictionary<string, ISongData> locals;
			lock (sdc.syncroot)
				locals = sdc.localSongs;
			if (!locals.TryGetValue(path, out retval))
				retval = null;//not really necessary.
			return retval;
		}
	}
}