using System.Data.Common;
using System;
namespace LastFMspider.LastFMSQLiteBackend
{
    public class UpdateTrackInfo : AbstractLfmCacheQuery
    {
        public UpdateTrackInfo(LastFMSQLiteCache lfm)
            : base(lfm)
        {
            id = DefineParameter("@id");
            listeners = DefineParameter("@listeners");
            playcount = DefineParameter("@playcount");
            duration = DefineParameter("@duration");
            artistMbid = DefineParameter("@lowerArtistMbid");
            trackMbid = DefineParameter("@lowerTrackMbid");
            lastFmId = DefineParameter("@lastFmId");
            timestamp = DefineParameter("@timestamp");
        }

        protected override string CommandText
        {
            get { return @"UPDATE TrackInfo   SET Listeners=@listeners, InfoTimestamp=@timestamp, 
                                                  Playcount=@playcount, Duration=@duration,
                                                  ArtistMbidID=(SELECT MbidID FROM Mbid WHERE LowercaseMbid = @lowerArtistMbid), 
                                                  TrackMbidID=(SELECT MbidID FROM Mbid WHERE LowercaseMbid = @lowerTrackMbid),
                                                  LastFmId=@lastFmId
                                            WHERE TrackID=@id"; }
        }

        DbParameter id, listeners, playcount, duration, artistMbid, trackMbid, lastFmId, timestamp;

        public void Execute(TrackRow row, TrackInfoRow infoRow)
        {
            id.Value = row.TrackID;
            timestamp.Value = infoRow.InfoTimestamp == null ? DBNull.Value : (object)infoRow.InfoTimestamp.Value.Ticks;

            listeners.Value = infoRow.Listeners;
            playcount.Value = infoRow.Playcount;
            duration.Value = infoRow.Duration;
            artistMbid.Value = infoRow.ArtistMbidID
            CommandObj.ExecuteNonQuery();
        }
    }
}
