﻿using System;
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
		public LookupSimilarityList LookupSimilarityList { get; private set; }
		public LookupSimilarityListInfo LookupSimilarityListInfo { get; private set; }
		public InsertArtist InsertArtist { get; private set; }
		public InsertTrack InsertTrack { get; private set; }
		public LookupTrack LookupTrack { get; private set; }
		public LookupArtist LookupArtist { get; private set; }
		public LookupTrackID LookupTrackID { get; private set; }
		public InsertSimilarityList InsertSimilarityList { get; private set; }
		public AllTracks AllTracks { get; private set; }
		public TracksWithoutSimilarityList TracksWithoutSimilarityList { get; private set; }
		public RawArtists RawArtists { get; private set; }
		public RawTracks RawTracks { get; private set; }
		public UpdateTrackCasing UpdateTrackCasing { get; private set; }
		public UpdateArtistCasing UpdateArtistCasing { get; private set; }
		public LookupArtistSimilarityList LookupArtistSimilarityList { get; private set; }
		public LookupArtistSimilarityListInfo LookupArtistSimilarityListAge { get; private set; }
		public InsertArtistSimilarityList InsertArtistSimilarityList { get; private set; }
		public ArtistsWithoutSimilarityList ArtistsWithoutSimilarityList { get; private set; }
		public ArtistsWithoutTopTracksList ArtistsWithoutTopTracksList { get; private set; }
		public LookupArtistTopTracksListInfo LookupArtistTopTracksListAge { get; private set; }
		public LookupArtistTopTracksList LookupArtistTopTracksList { get; private set; }
		public InsertArtistTopTracksList InsertArtistTopTracksList { get; private set; }
		public SetArtistAlternate SetArtistAlternate { get; private set; }
		public LookupArtistInfo LookupArtistInfo { get; private set; }
		internal TrackSetCurrentSimList TrackSetCurrentSimList { get; private set; }
		internal ArtistSetCurrentTopTracks ArtistSetCurrentTopTracks { get; private set; }
		internal ArtistSetCurrentSimList ArtistSetCurrentSimList { get; private set; }

		public LastFMSQLiteCache(SongDatabaseConfigFile config) : this(LastFmDbBuilder.DbFile(config)) { }

		public LastFMSQLiteCache(FileInfo dbFile) {
			Connection = LastFmDbBuilder.ConstructConnection(dbFile);
			Connection.Open();
			try {
				LastFmDbBuilder.CreateTables(Connection);
			} catch (SQLiteException) { }//if we can't create, we just hope the tables are already OK.
			PrepareSql();
		}

		private void PrepareSql() {
			InsertTrack = new InsertTrack(this);
			LookupTrack = new LookupTrack(this);
			LookupArtist = new LookupArtist(this);
			LookupTrackID = new LookupTrackID(this);
			InsertSimilarityList = new InsertSimilarityList(this);
			InsertArtist = new InsertArtist(this);
			LookupSimilarityList = new LookupSimilarityList(this);
			LookupSimilarityListInfo = new LookupSimilarityListInfo(this);
			AllTracks = new AllTracks(this);
			RawTracks = new RawTracks(this);
			RawArtists = new RawArtists(this);
			UpdateTrackCasing = new UpdateTrackCasing(this);
			UpdateArtistCasing = new UpdateArtistCasing(this);
			TracksWithoutSimilarityList = new TracksWithoutSimilarityList(this);
			LookupArtistSimilarityListAge = new LookupArtistSimilarityListInfo(this);
			LookupArtistSimilarityList = new LookupArtistSimilarityList(this);
			InsertArtistSimilarityList = new InsertArtistSimilarityList(this);
			ArtistsWithoutSimilarityList = new ArtistsWithoutSimilarityList(this);
			ArtistsWithoutTopTracksList = new ArtistsWithoutTopTracksList(this);
			LookupArtistTopTracksListAge = new LookupArtistTopTracksListInfo(this);
			LookupArtistTopTracksList = new LookupArtistTopTracksList(this);
			InsertArtistTopTracksList = new InsertArtistTopTracksList(this);
			SetArtistAlternate = new SetArtistAlternate(this);
			TrackSetCurrentSimList = new TrackSetCurrentSimList(this);
			ArtistSetCurrentTopTracks = new ArtistSetCurrentTopTracks(this);
			ArtistSetCurrentSimList = new ArtistSetCurrentSimList(this);
			LookupArtistInfo = new LookupArtistInfo(this);
		}


		public void Dispose() {
			lock (SyncRoot) {
				if (Connection != null)
					Connection.Dispose();
			}
		}
	}

}
