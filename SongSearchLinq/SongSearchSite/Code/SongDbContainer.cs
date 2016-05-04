using System;
using System.Collections.Concurrent;
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
using LastFMspider.LastFMSQLiteBackend;
using SongDataLib;
//using SuffixTreeLib;

namespace SongSearchSite {

	public sealed class SongDbContainer : IDisposable {
		const string songsPrefix = "songs/";
		static string CanonicalRelativeSongPath(Uri localSongPath) {
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

		static string EscapedRelativeSongPath(Uri localSongPath) {//failure to manually escape this means .NET interprets ? + # as valid uri segments and generates a valid uri - but not the one intended.
			return Uri.EscapeDataString(CanonicalRelativeSongPath(localSongPath)).Replace("%2F", "/");
		}

		public static Uri SongBaseUri(HttpContext context) {
			return new Uri(context.Request.Url, context.Request.ApplicationPath.TrimEnd('/') + "/" + songsPrefix);
		}

		public static Uri AppBaseUri(HttpContext context) {
			return new Uri(context.Request.Url, context.Request.ApplicationPath.TrimEnd('/') + "/");
		}

		public static Func<Uri, Uri> LocalSongPathToAbsolutePathMapper(HttpContext context) {
			Uri songsBaseUri = SongBaseUri(context);
			return localSongUri => localSongUri.IsFile ? new Uri(songsBaseUri, EscapedRelativeSongPath(localSongUri)) : localSongUri;
		}

		public static Func<Uri, Uri> LocalSongPathToAppRelativeMapper(HttpContext context) {
			Uri appBaseUri = AppBaseUri(context);
			Uri songsBaseUri = SongBaseUri(context);
			return localSongUri => localSongUri.IsFile ? appBaseUri.MakeRelativeUri(new Uri(songsBaseUri, EscapedRelativeSongPath(localSongUri))) : localSongUri;
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

		Task<SearchableSongFiles> searchEngine;
		SongTools tools;
		Task<FuzzySongSearcher> fuzzySearcher;
		Task<Func<TrackId, Tuple<SongRef, SongFileData>>> fuzzyTrackLookup;
		PlaylistDb playlistDb;
		ConcurrentDictionary<SortOrdering, int[]> rankMap;

		Task<SortedList<string, SongFileData>> localSongs;
		FileSystemWatcher fsWatcher;
		public void Dispose() {
			if (fsWatcher != null)
				fsWatcher.Dispose();
		}

		bool isFresh;
		DateTime loaded;

		void Init() {
			if (isFresh)
				return;
			isFresh = true;
			tools = new SongTools();
			if (fsWatcher == null) {
				fsWatcher = new FileSystemWatcher {
					Path = tools.ConfigFile.DataDirectory.FullName,
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
			loaded = DateTime.UtcNow;
			var searchData = Task.Factory.StartNew(() => tools.SongFilesSearchData);
			rankMap = new ConcurrentDictionary<SortOrdering, int[]>();

			searchEngine = searchData.ContinueWith(sd => new SearchableSongFiles(sd.Result, null));
			fuzzySearcher = searchData.ContinueWith(sd => new FuzzySongSearcher(tools.SongFilesSearchData.Songs));
			fuzzyTrackLookup = fuzzySearcher.ContinueWith(searcherTask => {
				var uncached = FindSimilarPlaylist.UncachedFuzzySearch(tools, searcherTask.Result);
				var cache = new ConcurrentDictionary<TrackId, Tuple<SongRef, SongFileData>>();

				return (Func<TrackId, Tuple<SongRef, SongFileData>>)(trackid => cache.GetOrAdd(trackid, uncached));
			});

			playlistDb = new PlaylistDb(tools.ConfigFile);
			localSongs = searchData.ContinueWith(sd =>
				new SortedList<string, SongFileData>(
					tools.SongFilesSearchData.Songs.Where(s => s.IsLocal)
					.ToDictionary(song => CanonicalRelativeSongPath(song.SongUri))
					)
				);
		}

		void DbUpdated() {
			isFresh = false;
			ThreadPool.QueueUserWorkItem(o => {
				Thread.Sleep(5000);
				Init();
			});
		}

		int[] RankMapForOrdering(SortOrdering ordering) {
			return rankMap.GetOrAdd(ordering, o => SortOrder.RankMapFor(searchEngine.Result.db.songs, o));
		}


		private SongDbContainer() { Init(); }
		public static SearchableSongFiles SearchableSongDB { get { return Singleton.searchEngine.Result; } }
		public static FuzzySongSearcher FuzzySongSearcher { get { return Singleton.fuzzySearcher.Result; } }
		public static Func<TrackId, Tuple<SongRef, SongFileData>> MemoizedFuzzyTrackLookup { get { return Singleton.fuzzyTrackLookup.Result; } }

		public static SongTools LastFmTools { get { return Singleton.tools; } }
		public static PlaylistDb PlaylistDb { get { return Singleton.playlistDb; } }
		public static DateTime LoadTime { get { return Singleton.loaded; } }
		public static int[] RankMapFor(SortOrdering ordering) { return Singleton.RankMapForOrdering(ordering); }

		/// <summary>
		/// Determines whether a given path maps to an indexed, local song.  If it doesn't, it returns null.  If it does, it returns the meta data known about the song, including the song's "real" path.
		/// </summary>
		/// <param name="path">The normalized path </param>
		/// <returns></returns>
		static SongFileData GetSongByNormalizedPath(string path) {
			SongFileData retval;
			Singleton.localSongs.Result.TryGetValue(path, out retval);
			return retval;
		}

		/// <summary>
		/// Determines whether a given path maps to an indexed, local song.  If it doesn't, it returns null.  If it does, it returns the meta data known about the song, including the song's "real" path.
		/// </summary>
		/// <param name="appBaseUri"></param>
		/// <param name="reqPath">Application relative request path</param>
		public static SongFileData GetSongFromFullUri(Uri appBaseUri, string reqPath) {
			//reqPath might be relative or absolute.
			Uri songUri = new Uri(appBaseUri, reqPath);
			string appRelativeUri = Uri.UnescapeDataString(appBaseUri.MakeRelativeUri(songUri).ToString());
			if (!appRelativeUri.StartsWith(songsPrefix))
				return null;

			string songNormedPath = appRelativeUri.Substring(songsPrefix.Length);
			return GetSongByNormalizedPath(songNormedPath);
		}

	}
}