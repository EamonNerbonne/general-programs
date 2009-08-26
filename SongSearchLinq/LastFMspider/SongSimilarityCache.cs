using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using LastFMspider.LastFMSQLiteBackend;
using System.Xml;

using EmnExtensions.Text;
using EmnExtensions.Web;
using EmnExtensions.Collections;
using EmnExtensions;
using System.Diagnostics;
using SongDataLib;
using LastFMspider.OldApi;

namespace LastFMspider
{
	public class SongSimilarityCache
	{
		public LastFMSQLiteCache backingDB { get; private set; }

		public SongSimilarityCache(SongDatabaseConfigFile configFile) {
			Init(configFile);
		}


		private void Init() {
			Console.WriteLine("Loading config...");
			var configFile = new SongDatabaseConfigFile(false);
			Init(configFile);
		}

		private void Init(SongDatabaseConfigFile configFile) {
			Console.WriteLine("Initializing sqlite db");
			backingDB = new LastFMSQLiteCache(configFile);//TODO decide what kind of DB we really want...
		}

		public SongSimilarityList Lookup(SongRef songref) { return Lookup(songref, TimeSpan.FromDays(365.0)); }

		public SongSimilarityList Lookup(SongRef songref, TimeSpan maxAge) {
			TrackSimilarityListInfo cachedVersion = backingDB.LookupSimilarityListAge.Execute(songref);
			if (!cachedVersion.ListID.HasValue || !cachedVersion.LookupTimestamp.HasValue || cachedVersion.LookupTimestamp.Value < DateTime.UtcNow - maxAge) { //get online version
				Console.Write("?");
				var retval = OldApiClient.Track.GetSimilarTracks(songref);
				try {
					backingDB.InsertSimilarityList.Execute(retval);
				} catch {//retry; might be a locking issue.  only retry once.
					System.Threading.Thread.Sleep(100);
					backingDB.InsertSimilarityList.Execute(retval);
				}
				return retval;
			} else {
				return backingDB.LookupSimilarityList.Execute( cachedVersion);
			}
		}
	}
}
