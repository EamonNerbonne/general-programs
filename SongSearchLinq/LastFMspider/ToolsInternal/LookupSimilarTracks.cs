
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LastFMspider.LastFMSQLiteBackend;
using LastFMspider.OldApi;
using SongDataLib;

namespace LastFMspider {
	public static class LookupSimilarTracksHelper {
		static readonly TimeSpan normalMaxAge = TimeSpan.FromDays(365.0);

		public static SongSimilarityList Lookup(SongTools tools, SongRef songref, TimeSpan maxAge = default(TimeSpan)) {
			return Lookup(tools, songref, tools.LastFmCache.LookupSimilarityListInfo.Execute(songref), maxAge);
		}
		public static SongSimilarityList Lookup(SongTools tools, SongRef songref, TrackSimilarityListInfo cachedVersion, TimeSpan maxAge = default(TimeSpan)) {
			if (!IsFresh(cachedVersion, maxAge))  //get online version
				return DoWebLookup(tools.LastFmCache, songref);
			else
				return tools.LastFmCache.LookupSimilarityList.Execute(cachedVersion);
		}

		static readonly object syncInFlight = new object();
		static readonly Dictionary<SongRef, Task<SongSimilarityList>> inFlight = new Dictionary<SongRef, Task<SongSimilarityList>>();
		static int lookupsDone;
		public static int WebLookupsSoFar() { return lookupsDone; }

		static SongSimilarityList DoWebLookup(LastFMSQLiteCache backingDB, SongRef songref) {
			Console.Write("?" + songref);
			Task<SongSimilarityList> lookupTask;
			lock (syncInFlight) {
				if (!inFlight.TryGetValue(songref, out lookupTask)) {
					lookupTask = Task.Factory.StartNew(() => OldApiClient.Track.GetSimilarTracks(songref));
					lookupTask.ContinueWith(task => {
						Console.WriteLine(" [" + task.Result.similartracks.Length + "]");
						Interlocked.Increment(ref lookupsDone);
						try {
							backingDB.InsertSimilarityList.Execute(task.Result);
						} catch {//retry; might be a locking issue.  only retry once.
							Thread.Sleep(100);
							backingDB.InsertSimilarityList.Execute(task.Result);
						}
					}).ContinueWith(task => {
						Task removeTask = new Task(() => {
							lock (syncInFlight)
								inFlight.Remove(songref);
						});
						Timer timer = new Timer(ignore => removeTask.Start(), null, 5000, Timeout.Infinite);
						removeTask.ContinueWith(ignore => timer.Dispose());
					});
					inFlight[songref] = lookupTask;
				}
			}
			return lookupTask.Result;
		}

		public static bool IsFresh(TrackSimilarityListInfo cachedVersion, TimeSpan maxAge = default(TimeSpan)) {
			if (maxAge == default(TimeSpan)) maxAge = normalMaxAge;
			return cachedVersion.ListID.HasValue && cachedVersion.LookupTimestamp.HasValue && cachedVersion.LookupTimestamp.Value >= DateTime.UtcNow - maxAge;
		}

		public static void RefreshCache(LastFMSQLiteCache backingDB, TrackId track, TimeSpan maxAge = default(TimeSpan))
		{
			RefreshCache(backingDB, backingDB.LookupSimilarityListInfo.Execute(track), maxAge);
		}

		public static void RefreshCache(LastFMSQLiteCache backingDB, TrackSimilarityListInfo list, TimeSpan maxAge = default(TimeSpan))
		{
			if (!IsFresh(list, maxAge)) {
				var songref = backingDB.LookupTrack.Execute(list.TrackId);
				if (songref != null)
					DoWebLookup(backingDB, songref);
			}
		}
	}
}
