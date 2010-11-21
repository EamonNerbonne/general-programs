using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
    public class UpdateTrackCasing: AbstractLfmCacheQuery {
        public UpdateTrackCasing(LastFMSQLiteCache lfm)
            : base(lfm) {
            lowerTitle = DefineParameter("@lowerTitle");
            fullTitle = DefineParameter("@fullTitle");
			artistId = DefineParameter("@artistId");
		}
        protected override string CommandText {
            get {
                return @"
UPDATE Track SET FullTitle = @fullTitle WHERE ArtistID = @artistId AND LowercaseTitle=@lowerTitle;

INSERT OR IGNORE INTO [Track] (ArtistID, FullTitle, LowercaseTitle)
VALUES (@artistId, @fullTitle, @lowerTitle);

SELECT TrackID FROM Track  where ArtistID = @artistId AND LowercaseTitle=@lowerTitle
";
            }
        }
        
        readonly DbParameter lowerTitle,  fullTitle, artistId;

        public TrackId Execute(SongRef songRef) {
            lock (SyncRoot) {
				artistId.Value = lfmCache.UpdateArtistCasing.Execute(songRef.Artist).id;
                lowerTitle.Value = songRef.Title.ToLatinLowercase();
                fullTitle.Value = songRef.Title;
				return new TrackId(CommandObj.ExecuteScalar().CastDbObjectAs<long>());
            }
        }
    }
}
