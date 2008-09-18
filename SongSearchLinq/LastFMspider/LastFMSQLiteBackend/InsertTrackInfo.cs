using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend
{
    public class InsertTrackInfo : AbstractLfmCacheQuery
    {
        public InsertTrackInfo(LastFMSQLiteCache lfm)
            : base(lfm)
        {
            lowerArtist = DefineParameter("@lowerArtist");
            lowerTrack = DefineParameter("@lowerTrack");
            listeners = DefineParameter("@listeners");
            playcount = DefineParameter("@playcount");
            duration = DefineParameter("@duration");
            artistMbid = DefineParameter("@lowerArtistMbid");
            trackMbid = DefineParameter("@lowerTrackMbid");
            lastFmId = DefineParameter("@lastFmId");
        }

        protected override string CommandText
        {
            get
            {
                return @"
INSERT OR IGNORE INTO [TrackInfo] (TrackID, Listeners, Playcount, Duration, ArtistMbidID, TrackMbidID, LastFmId)
SELECT TR.TrackID, @listeners, @playcount, @duration, A.MbidID, T.MbidID, @lastFmId FROM Artist AR, Track TR, Mbid A, Mbid T
WHERE AR.LowercaseArtist = @lowerArtist AND TR.ArtistID = AR.ArtistID 
  AND A.LowercaseMbid = @lowerArtistMbid 
  AND T.LowercaseMbid = @lowerTrackMbid
";
            }
        }
        DbParameter lowerArtist, lowerTrack, listeners, playcount, duration, artistMbid, trackMbid, lastFmId;

        public void Execute(SongRef songref)
        {
            lowerTrack.Value = songref.Title.ToLowerInvariant();
            lowerArtist.Value = songref.Artist.ToLowerInvariant();
            CommandObj.ExecuteNonQuery();
        }

    }
}
