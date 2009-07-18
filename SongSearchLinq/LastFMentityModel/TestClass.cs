using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Objects;
using EmnExtensions.DebugTools;
using System.Diagnostics;
using EmnExtensions;
using System.Data.EntityClient;
using System.Data.Common;
using LastFMspider;
using SongDataLib;
using System.Reflection;
using System.IO;

namespace LastFMentityModel
{
	static class TestClass
	{


		private static string PrintDebug(object obj) {
			if (obj is ObjectQuery) {
				var queryO = (ObjectQuery)obj;
				return string.Format("{0}\n\n{1}", queryO.ToTraceString(), string.Join("\n", queryO.Parameters.Select(p => string.Format("{0} {1} = {2};", p.ParameterType, p.Name, p.Value)).ToArray()));
			} else if (obj == null) {
				return "<null>";
			} else
				return obj.ToString();
		}

		public static void PrintTimeRes(this Stopwatch timer, string msg, object obj) {
			try {
				timer.Stop();
				Console.WriteLine(PrintDebug(obj));
				Console.WriteLine("{1} Took: {0}", timer.Elapsed.TotalMilliseconds, msg);
			} finally {
				timer.Reset();
				timer.Start();
			}
		}


		class revsort<T> : IComparer<T> where T : IComparable<T> { public int Compare(T x, T y) { return y.CompareTo(x); } }

		static void Test() {
			using (LastFMCacheModel model = new LastFMCacheModel()) {

				Stopwatch timer = Stopwatch.StartNew();
				var toptrackReach = from toptrack in model.TopTracks
									orderby toptrack.InList.ListID
									select new { toptrack.Reach, ListID = toptrack.InList.ListID };
				PrintTimeRes(timer, "toptrack query building", toptrackReach);

				var artistTTLs = from artist in model.Artist
								 where artist.CurrentTopTracksList != null
								 select new { artist.ArtistID, artist.CurrentTopTracksList.ListID };
				PrintTimeRes(timer, "artist query building", artistTTLs);

				var curTTL = artistTTLs.ToDictionary(artist => (uint)artist.ListID, artist => (uint)artist.ArtistID);
				PrintTimeRes(timer, "artist query exec", curTTL.Count + " artists mapped total.");

				long[] reach = new long[curTTL.Values.Max() + 1];
				uint[] artistIDs = Enumerable.Range(0, reach.Length).Select(i => (uint)i).ToArray();
				PrintTimeRes(timer, "reach aggregation arrays created", reach.Length + " elements.");

				uint itemCount = 0;
				foreach (var entry in toptrackReach) {
					itemCount++;
					uint artistID;
					if (curTTL.TryGetValue((uint)entry.ListID, out artistID)) {
						reach[artistID] += entry.Reach;
					}
				}

				PrintTimeRes(timer, "Reach computed", itemCount + " items processed.");

				Array.Sort(reach, artistIDs, new revsort<long>());

				PrintTimeRes(timer, "Reach sorted.", "");


				/*group toptrack.Reach by toptrack.InList.ListID into g
				let artID = g.Key
				let reach = g.Sum()
				orderby reach descending
				select new { ArtistID = artID, ArtistReachEstimate = reach };*/
				long curArtistID = -1;
				var aL = from artist in model.Artist
						 where artist.ArtistID == curArtistID
						 select artist;
				for (int i = 0; i < 1000; i++) {
					curArtistID = artistIDs[i];
					var a = aL.First();
					Console.WriteLine("{2}: {0} (Reach:{1})", a.FullArtist, reach[i], i + 1);
				}
				PrintTimeRes(timer, "Query execution", "");
			}
		}


		static void Main(string[] args) {
			new LastFMSQLiteCache(new SongDatabaseConfigFile(false)).Dispose();//just to create the appropriate tables.
			using (var model = new LastFMCacheModel())
				Actions.LfmAction.EnsureLocalFilesInDb(new SimpleSongDB(new SongDatabaseConfigFile(false), null), model);
			Console.WriteLine("done.");
			Console.ReadKey();
		}

		const string DataProvider = "System.Data.SQLite";
		const string DataConnectionString = "page size=4096;cache size=100000;datetimeformat=Ticks;Legacy Format=False;Synchronous=Off;data source=\"{0}\"";

		/// <summary>
		/// Effectively:
		/// select TT.TrackID, A.FullArtist, T.FullTitle,  TT.Reach, TTL.LookupTimeStamp
		/// from TopTracks TT, TopTracksList TTL, Track T, Artist A
		/// where TT.Reach > 30000
		/// AND TT.ListID = TTL.ListID
		/// AND TTL.LookupTimestamp > 633672352317986158
		/// AND TTL.ArtistID = A.ArtistID
		/// AND TTL.ListID = A.CurrentTopTracksList
		/// AND T.TrackID = TT.TrackID
		/// order by TT.TrackID desc
		/// </summary>
		static void Test2() {
			using (LastFMSQLiteCache cache = new LastFMSQLiteCache(new FileInfo(@"E:\musicDB\cache\lastFMcache.old.s3db"))) {
				cache.Connection.Close();

				using (LastFMCacheModel model = new LastFMCacheModel()) { //new LastFMCacheModel(new EntityConnection(new System.Data.Metadata.Edm.MetadataWorkspace(new[] { "res://*/" }, new[] { Assembly.GetExecutingAssembly() }), cache.Connection))) {
					Stopwatch timer = Stopwatch.StartNew();
					//var AFewMonthsAgo = DateTime.Now - TimeSpan.FromDays(365.25/12.0*6.0);
					var AFewMonthsAgo = new DateTime(633672352317986158L);//2009-01-12
					var relevantTracks = from toptrack in model.TopTracks
										 orderby toptrack.Track.TrackID descending
										 where toptrack.Reach > 30000 && toptrack.InList.LookupTimestamp > AFewMonthsAgo.Ticks && toptrack.InList == toptrack.InList.OfArtist.CurrentTopTracksList
										 orderby toptrack.Track.TrackID descending
										 select new { toptrack.Track.TrackID, FullArtist = toptrack.Track.Artist.FullArtist, FullTitle = toptrack.Track.FullTitle, Reach = toptrack.Reach, LookupTimestamp = toptrack.InList.LookupTimestamp };
					PrintTimeRes(timer, "relevant track query constructed", relevantTracks);
					int i = 0;
					foreach (var recenttrack in relevantTracks) {
						Console.WriteLine("{0}:{5}: {1} - {2} ({3} listeners @ {4})", ++i, recenttrack.FullArtist, recenttrack.FullTitle, recenttrack.Reach, new DateTime(recenttrack.LookupTimestamp), recenttrack.TrackID);
					}
					PrintTimeRes(timer, "Query execution", "");

				}
			}
		}
	}
}
