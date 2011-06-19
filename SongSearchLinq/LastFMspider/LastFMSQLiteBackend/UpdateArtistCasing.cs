using System.Data.Common;
using SongDataLib;

namespace LastFMspider.LastFMSQLiteBackend
{
    public class UpdateArtistCasing : AbstractLfmCacheQuery
    {
        public UpdateArtistCasing(LastFMSQLiteCache lfm)
            : base(lfm) {
            lowerArtist = DefineParameter("@lowerArtist");
            fullArtist = DefineParameter("@fullArtist");
        }
        protected override string CommandText {
            get {
                return @"
UPDATE Artist SET FullArtist = @fullArtist WHERE LowercaseArtist=@lowerArtist;

INSERT OR IGNORE INTO [Artist] (FullArtist, LowercaseArtist)
VALUES (@fullArtist, @lowerArtist);

SELECT ArtistID FROM Artist where LowercaseArtist=@lowerArtist
";
            }
        }

        readonly DbParameter  lowerArtist,  fullArtist;

        public ArtistId Execute(string artistName) {
            lock (SyncRoot) {
                lowerArtist.Value = artistName.ToLatinLowercase();
                fullArtist.Value = artistName;
                return new ArtistId( CommandObj.ExecuteScalar().CastDbObjectAs<long>());
            }
        }

    }
}
