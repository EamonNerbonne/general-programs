using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.Common;
using LastFMspider.LastFMSQLiteBackend;
using System.Xml.Linq;
using SongDataLib;
using System.Data.SQLite;

namespace LastFMspider {
	public sealed class LastFMSQLiteCache : IDisposable {
		public readonly object SyncRoot = new object();

		public DbConnection Connection { get; private set; }

		LookupSimilarityList c_LookupSimilarityList;
		public LookupSimilarityList LookupSimilarityList { get { lock (SyncRoot) return c_LookupSimilarityList ?? (c_LookupSimilarityList = new LookupSimilarityList(this)); } }
		LookupSimilarityListInfo c_LookupSimilarityListInfo;
		public LookupSimilarityListInfo LookupSimilarityListInfo { get { lock (SyncRoot) return c_LookupSimilarityListInfo ?? (c_LookupSimilarityListInfo = new LookupSimilarityListInfo(this)); } }
		InsertArtist c_InsertArtist;
		public InsertArtist InsertArtist { get { lock (SyncRoot) return c_InsertArtist ?? (c_InsertArtist = new InsertArtist(this)); } }
		InsertTrack c_InsertTrack;
		public InsertTrack InsertTrack { get { lock (SyncRoot) return c_InsertTrack ?? (c_InsertTrack = new InsertTrack(this)); } }
		LookupTrack c_LookupTrack;
		public LookupTrack LookupTrack { get { lock (SyncRoot) return c_LookupTrack ?? (c_LookupTrack = new LookupTrack(this)); } }
		LookupArtist c_LookupArtist;
		public LookupArtist LookupArtist { get { lock (SyncRoot) return c_LookupArtist ?? (c_LookupArtist = new LookupArtist(this)); } }
		LookupTrackID c_LookupTrackID;
		public LookupTrackID LookupTrackID { get { lock (SyncRoot) return c_LookupTrackID ?? (c_LookupTrackID = new LookupTrackID(this)); } }
		InsertSimilarityList c_InsertSimilarityList;
		public InsertSimilarityList InsertSimilarityList { get { lock (SyncRoot) return c_InsertSimilarityList ?? (c_InsertSimilarityList = new InsertSimilarityList(this)); } }
		AllTracks c_AllTracks;
		public AllTracks AllTracks { get { lock (SyncRoot) return c_AllTracks ?? (c_AllTracks = new AllTracks(this)); } }
		TracksWithoutSimilarityList c_TracksWithoutSimilarityList;
		public TracksWithoutSimilarityList TracksWithoutSimilarityList { get { lock (SyncRoot) return c_TracksWithoutSimilarityList ?? (c_TracksWithoutSimilarityList = new TracksWithoutSimilarityList(this)); } }
		RawArtists c_RawArtists;
		public RawArtists RawArtists { get { lock (SyncRoot) return c_RawArtists ?? (c_RawArtists = new RawArtists(this)); } }
		RawTracks c_RawTracks;
		public RawTracks RawTracks { get { lock (SyncRoot) return c_RawTracks ?? (c_RawTracks = new RawTracks(this)); } }
		UpdateTrackCasing c_UpdateTrackCasing;
		public UpdateTrackCasing UpdateTrackCasing { get { lock (SyncRoot) return c_UpdateTrackCasing ?? (c_UpdateTrackCasing = new UpdateTrackCasing(this)); } }
		UpdateArtistCasing c_UpdateArtistCasing;
		public UpdateArtistCasing UpdateArtistCasing { get { lock (SyncRoot) return c_UpdateArtistCasing ?? (c_UpdateArtistCasing = new UpdateArtistCasing(this)); } }
		LookupArtistSimilarityList c_LookupArtistSimilarityList;
		public LookupArtistSimilarityList LookupArtistSimilarityList { get { lock (SyncRoot) return c_LookupArtistSimilarityList ?? (c_LookupArtistSimilarityList = new LookupArtistSimilarityList(this)); } }
		LookupArtistSimilarityListInfo c_LookupArtistSimilarityListAge;
		public LookupArtistSimilarityListInfo LookupArtistSimilarityListAge { get { lock (SyncRoot) return c_LookupArtistSimilarityListAge ?? (c_LookupArtistSimilarityListAge = new LookupArtistSimilarityListInfo(this)); } }
		InsertArtistSimilarityList c_InsertArtistSimilarityList;
		public InsertArtistSimilarityList InsertArtistSimilarityList { get { lock (SyncRoot) return c_InsertArtistSimilarityList ?? (c_InsertArtistSimilarityList = new InsertArtistSimilarityList(this)); } }
		ArtistsWithoutSimilarityList c_ArtistsWithoutSimilarityList;
		public ArtistsWithoutSimilarityList ArtistsWithoutSimilarityList { get { lock (SyncRoot) return c_ArtistsWithoutSimilarityList ?? (c_ArtistsWithoutSimilarityList = new ArtistsWithoutSimilarityList(this)); } }
		ArtistsWithoutTopTracksList c_ArtistsWithoutTopTracksList;
		public ArtistsWithoutTopTracksList ArtistsWithoutTopTracksList { get { lock (SyncRoot) return c_ArtistsWithoutTopTracksList ?? (c_ArtistsWithoutTopTracksList = new ArtistsWithoutTopTracksList(this)); } }
		LookupArtistTopTracksListInfo c_LookupArtistTopTracksListAge;
		public LookupArtistTopTracksListInfo LookupArtistTopTracksListAge { get { lock (SyncRoot) return c_LookupArtistTopTracksListAge ?? (c_LookupArtistTopTracksListAge = new LookupArtistTopTracksListInfo(this)); } }
		LookupArtistTopTracksList c_LookupArtistTopTracksList;
		public LookupArtistTopTracksList LookupArtistTopTracksList { get { lock (SyncRoot) return c_LookupArtistTopTracksList ?? (c_LookupArtistTopTracksList = new LookupArtistTopTracksList(this)); } }
		InsertArtistTopTracksList c_InsertArtistTopTracksList;
		public InsertArtistTopTracksList InsertArtistTopTracksList { get { lock (SyncRoot) return c_InsertArtistTopTracksList ?? (c_InsertArtistTopTracksList = new InsertArtistTopTracksList(this)); } }
		SetArtistAlternate c_SetArtistAlternate;
		public SetArtistAlternate SetArtistAlternate { get { lock (SyncRoot) return c_SetArtistAlternate ?? (c_SetArtistAlternate = new SetArtistAlternate(this)); } }
		LookupArtistInfo c_LookupArtistInfo;
		public LookupArtistInfo LookupArtistInfo { get { lock (SyncRoot) return c_LookupArtistInfo ?? (c_LookupArtistInfo = new LookupArtistInfo(this)); } }
		TrackSetCurrentSimList c_TrackSetCurrentSimList;
		internal TrackSetCurrentSimList TrackSetCurrentSimList { get { lock (SyncRoot) return c_TrackSetCurrentSimList ?? (c_TrackSetCurrentSimList = new TrackSetCurrentSimList(this)); } }
		ArtistSetCurrentTopTracks c_ArtistSetCurrentTopTracks;
		internal ArtistSetCurrentTopTracks ArtistSetCurrentTopTracks { get { lock (SyncRoot) return c_ArtistSetCurrentTopTracks ?? (c_ArtistSetCurrentTopTracks = new ArtistSetCurrentTopTracks(this)); } }
		ArtistSetCurrentSimList c_ArtistSetCurrentSimList;
		internal ArtistSetCurrentSimList ArtistSetCurrentSimList { get { lock (SyncRoot) return c_ArtistSetCurrentSimList ?? (c_ArtistSetCurrentSimList = new ArtistSetCurrentSimList(this)); } }

		public LastFMSQLiteCache(SongDataConfigFile config, bool suppressCreation = false) : this(LastFmDbBuilder.DbFile(config), suppressCreation) { }

		public LastFMSQLiteCache(FileInfo dbFile, bool suppressCreation) {
			Connection = LastFmDbBuilder.ConstructConnection(dbFile);
			Connection.Open();
			if (!suppressCreation)
				try { LastFmDbBuilder.CreateTables(Connection); } catch (SQLiteException) { }//if we can't create, we just hope the tables are already OK.
		}

		public void Dispose() {
			lock (SyncRoot) {
				if (Connection != null)
					Connection.Dispose();
			}
		}
	}

}
