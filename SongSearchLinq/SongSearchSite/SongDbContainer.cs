using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using EmnExtensions.Text;
using LastFMspider;
using SongDataLib;
using SuffixTreeLib;

namespace SongSearchSite {

	public sealed class SongDbContainer : IDisposable {
		const string songsPrefix = "songs/";
		public static string CanonicalRelativeSongPath(Uri localSongPath) {
			StringBuilder sb = new StringBuilder();
			foreach (char c in localSongPath.LocalPath) {
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
						sb.Append(Convert.ToString(c, 16).PadLeft(2, '0'));
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

		public static string EscapedRelativeSongPath(Uri localSongPath) {//failure to manually escape this means .NET interprets ? + # as valid uri segments and generates a valid uri - but not the one intended.
			return Uri.EscapeDataString(CanonicalRelativeSongPath(localSongPath)).Replace("%2F", "/");
		}


		public static Func<Uri, Uri> LocalSongPathToAbsolutePathMapper(HttpContext context) {
			return local2absHelper(new Uri(context.Request.Url, context.Request.ApplicationPath + "/" + songsPrefix));
		}
		static Func<Uri, Uri> local2absHelper(Uri songsBaseUri) {
			return localSongUri => 
				localSongUri.IsFile ? new Uri(songsBaseUri, EscapedRelativeSongPath(localSongUri)) : localSongUri;
		}
		public static Func<Uri, Uri> LocalSongPathToAppRelativeMapper(HttpContext context) {
			return local2relHelper(new Uri(context.Request.Url, context.Request.ApplicationPath + "/" + songsPrefix), new Uri(context.Request.Url, context.Request.ApplicationPath + "/"));
		}
		static Func<Uri, Uri> local2relHelper(Uri songsBaseUri, Uri appBaseUri) {
			return localSongUri => 
				localSongUri.IsFile ? appBaseUri.MakeRelativeUri(new Uri(songsBaseUri, EscapedRelativeSongPath(localSongUri))) : localSongUri;
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
		SearchableSongFiles searchEngine;
		LastFmTools tools;
		FuzzySongSearcher fuzzySearcher;

		Dictionary<string, SongFileData> localSongs = new Dictionary<string, SongFileData>();
		readonly object syncroot = new object();
		FileSystemWatcher fsWatcher;
		public void Dispose() {
			if (fsWatcher != null)
				fsWatcher.Dispose();
		}

		bool isFresh;

		private void Init() {
			lock (syncroot) {
				if (isFresh)
					return;
				isFresh = true;
				SongDataConfigFile dcf = new SongDataConfigFile(true);
				tools = new LastFmTools(dcf);

				var allSongs = tools.SongsOnDisk.Songs;
				Array.Sort(allSongs,(a, b) => b.popularity.TitlePopularity.CompareTo(a.popularity.TitlePopularity));
				Parallel.Invoke(
					() => { fuzzySearcher = new FuzzySongSearcher(allSongs); },
					() => { searchEngine = new SearchableSongFiles(new SongFilesSearchData(allSongs), new SuffixTreeSongSearcher()); },
					() => { localSongs = allSongs.Where(s => s.IsLocal).ToDictionary(song => CanonicalRelativeSongPath(song.SongUri)); });
				if (fsWatcher == null) {
					fsWatcher = new FileSystemWatcher {
						Path = dcf.DataDirectory.FullName,
						Filter = "*.xml",
						IncludeSubdirectories = false,
					};
					fsWatcher.Created += (o, e) => DbUpdated();
					fsWatcher.Changed += (o, e) => DbUpdated();
					fsWatcher.Renamed += (o, e) => DbUpdated();
					fsWatcher.Error += (o, e) => DbUpdated();
					fsWatcher.Deleted += (o, e) => DbUpdated();
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
		public static SearchableSongFiles SearchableSongDB { get { var sdc = Singleton; lock (sdc.syncroot) return sdc.searchEngine; } }
		public static FuzzySongSearcher FuzzySongSearcher { get { var sdc = Singleton; lock (sdc.syncroot) return sdc.fuzzySearcher; } }
		public static LastFmTools LastFmTools { get { var sdc = Singleton; lock (sdc.syncroot) return sdc.tools; } }

		/// <summary>
		/// Determines whether a given path maps to an indexed, local song.  If it doesn't, it returns null.  If it does, it returns the meta data known about the song, including the song's "real" path.
		/// </summary>
		/// <param name="path">The normalized path </param>
		/// <returns></returns>
		public static ISongFileData GetSongByNormalizedPath(string path) {
			SongFileData retval;
			var sdc = Singleton;
			lock (sdc.syncroot)
				sdc.localSongs.TryGetValue(path, out retval);
			return retval;
		}

		/// <summary>
		/// Determines whether a given path maps to an indexed, local song.  If it doesn't, it returns null.  If it does, it returns the meta data known about the song, including the song's "real" path.
		/// </summary>
		/// <param name="reqPath">Application relative request path</param>
		public static ISongFileData GetSongFromFullUri(string reqPath) {
			if (!reqPath.StartsWith(songsPrefix))
				throw new Exception("Whoops, illegal request routing...  this should not be routed to this class!");

			string songNormedPath = reqPath.Substring(songsPrefix.Length);
			return GetSongByNormalizedPath(songNormedPath);
		}
	}
}