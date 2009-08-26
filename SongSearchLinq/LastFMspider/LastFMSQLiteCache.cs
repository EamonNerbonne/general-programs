using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.Common;
using System.Data.SQLite;
using LastFMspider.LastFMSQLiteBackend;
using System.Xml.Linq;
using SongDataLib;

namespace LastFMspider
{
	public sealed class LastFMSQLiteCache : IDisposable
	{
		public readonly object SyncRoot = new object();

		public DbConnection Connection { get; private set; }
		public LookupSimilarityList LookupSimilarityList { get; private set; }
		public LookupSimilarityListAge LookupSimilarityListAge { get; private set; }
		public InsertArtist InsertArtist { get; private set; }
		public InsertTrack InsertTrack { get; private set; }
		public LookupTrack LookupTrack { get; private set; }
		public LookupArtist LookupArtist { get; private set; }
		public LookupTrackID LookupTrackID { get; private set; }
		public InsertSimilarity InsertSimilarity { get; private set; }
		public InsertSimilarityList InsertSimilarityList { get; private set; }
		public CountSimilarities CountSimilarities { get; private set; }
		public CountRoughSimilarities CountRoughSimilarities { get; private set; }
		public AllTracks AllTracks { get; private set; }
		public TracksWithoutSimilarityList TracksWithoutSimilarityList { get; private set; }
		public RawArtists RawArtists { get; private set; }
		public RawTracks RawTracks { get; private set; }
		public UpdateTrackCasing UpdateTrackCasing { get; private set; }
		public UpdateArtistCasing UpdateArtistCasing { get; private set; }
		public RawSimilarTracks RawSimilarTracks { get; private set; }
		public LookupArtistSimilarityList LookupArtistSimilarityList { get; private set; }
		public LookupArtistSimilarityListAge LookupArtistSimilarityListAge { get; private set; }
		public InsertArtistSimilarity InsertArtistSimilarity { get; private set; }
		public InsertArtistSimilarityList InsertArtistSimilarityList { get; private set; }
		public ArtistsWithoutSimilarityList ArtistsWithoutSimilarityList { get; private set; }
		public ArtistsWithoutTopTracksList ArtistsWithoutTopTracksList { get; private set; }
		public LookupArtistTopTracksListAge LookupArtistTopTracksListAge { get; private set; }
		public LookupArtistTopTracksList LookupArtistTopTracksList { get; private set; }
		public InsertArtistTopTrack InsertArtistTopTrack { get; private set; }
		public InsertArtistTopTracksList InsertArtistTopTracksList { get; private set; }
		public SetArtistAlternate SetArtistAlternate { get; private set; }
		internal TrackSetCurrentSimList TrackSetCurrentSimList { get; private set; }
		internal ArtistSetCurrentTopTracks ArtistSetCurrentTopTracks { get; private set; }
		internal ArtistSetCurrentSimList ArtistSetCurrentSimList { get; private set; }

		public LastFMSQLiteCache(SongDatabaseConfigFile config) : this(LastFmDbBuilder.DbFile(config)) { }

		public LastFMSQLiteCache(FileInfo dbFile) {
			Connection = LastFmDbBuilder.ConstructConnection(dbFile);
			Connection.Open();
			LastFmDbBuilder.CreateTables(Connection);
			PrepareSql();
		}

		private void PrepareSql() {
			InsertTrack = new InsertTrack(this);
			LookupTrack = new LookupTrack(this);
			LookupArtist = new LookupArtist(this);
			LookupTrackID = new LookupTrackID(this);
			InsertSimilarityList = new InsertSimilarityList(this);
			InsertSimilarity = new InsertSimilarity(this);
			InsertArtist = new InsertArtist(this);
			LookupSimilarityList = new LookupSimilarityList(this);
			LookupSimilarityListAge = new LookupSimilarityListAge(this);
			CountSimilarities = new CountSimilarities(this);
			CountRoughSimilarities = new CountRoughSimilarities(this);
			AllTracks = new AllTracks(this);
			RawTracks = new RawTracks(this);
			RawArtists = new RawArtists(this);
			UpdateTrackCasing = new UpdateTrackCasing(this);
			UpdateArtistCasing = new UpdateArtistCasing(this);
			RawSimilarTracks = new RawSimilarTracks(this);
			TracksWithoutSimilarityList = new TracksWithoutSimilarityList(this);
			LookupArtistSimilarityListAge = new LookupArtistSimilarityListAge(this);
			LookupArtistSimilarityList = new LookupArtistSimilarityList(this);
			InsertArtistSimilarityList = new InsertArtistSimilarityList(this);
			InsertArtistSimilarity = new InsertArtistSimilarity(this);
			ArtistsWithoutSimilarityList = new ArtistsWithoutSimilarityList(this);
			ArtistsWithoutTopTracksList = new ArtistsWithoutTopTracksList(this);
			LookupArtistTopTracksListAge = new LookupArtistTopTracksListAge(this);
			LookupArtistTopTracksList = new LookupArtistTopTracksList(this);
			InsertArtistTopTrack = new InsertArtistTopTrack(this);
			InsertArtistTopTracksList = new InsertArtistTopTracksList(this);
			SetArtistAlternate = new SetArtistAlternate(this);
			TrackSetCurrentSimList = new TrackSetCurrentSimList(this);
			ArtistSetCurrentTopTracks = new ArtistSetCurrentTopTracks(this);
			ArtistSetCurrentSimList = new ArtistSetCurrentSimList(this);
		}


		public void Dispose() {
			lock (SyncRoot) {
				if (Connection != null)
					Connection.Dispose();
			}
		}
	}

}
