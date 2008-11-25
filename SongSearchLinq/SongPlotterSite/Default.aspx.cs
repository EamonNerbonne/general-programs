using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using LastFMspider;
using SimilarityMdsLib;
using System.Windows;

public partial class _Default : System.Web.UI.Page
{
    public static object syncroot = new object();
    public static MdsResults mds;
    [ThreadStatic]
    public static LastFmTools tools; //this is thread static due to Sqlite restrictions.
    void LoadStuff() {
        lock (syncroot)
            if (tools == null)
                tools = new LastFmTools(new SongDataLib.SongDatabaseConfigFile(false));
        if (mds == null) {
            mds = MdsResults.LoadResults(new MdsEngine.FormatAndOptions {
                Format = SimilarityFormat.AvgRank2,
                Options = new MdsEngine.Options {
                    NGenerations = 140,
                    LearnRate = 2.0,
                    StartAnnealingWhen = 0.0,
                    PointUpdateStyle = 1,
                    Dimensions = 2,
                }
            }, new SimCacheManager(SimilarityFormat.LastFmRating, tools, DataSetType.Training));
        }
    }
    PositionedSong[] songs = null;
    protected string[] unknownSongs = null;
    protected void Page_Load(object sender, EventArgs e) {
        if (!this.IsPostBack) return;
        LoadStuff();

        //so this is a postback.
        var playlist = LoadM3U.LoadPlaylistFromTextBlock(this.TextBox1.Text, tools);
        if (FileUpload1.HasFile)
            playlist = playlist.Concat(LoadM3U.LoadPlaylistFromM3U(this.FileUpload1.FileContent, tools));

        //using (var m3ustream = File.OpenRead(openDialog.FileName)) {
        PositionedTracks.PositionSongs(playlist, mds, out songs, out unknownSongs);
        if (unknownSongs.Length == 0) unknownSongs = null;
        TextBox1.Text = string.Join("\n" ,songs.Select(song => song.Song.ToString()).ToArray());
        if (unknownSongs != null)
            UnknownBox2.Text = string.Join("\n", unknownSongs);
        
        PositionedTracks.RepositionWithin(songs, new Rect(0, 0, 100.0, 100.0));


        //songCanvas.SetSongs(songs);
        //}
    }
    protected override void OnUnload(EventArgs e) {
        base.OnUnload(e);
        LoadStuff();
        songs = null;
        if (unknownSongs != null) {
            var backingDB = tools.SimilarSongs.backingDB;
            try {
                using (var trans = backingDB.Connection.BeginTransaction()) {
                    foreach (var songref in unknownSongs.SelectMany(unknownSong => LoadM3U.PossibleSongRefs(unknownSong)))
                        backingDB.InsertTrack.Execute(songref);
                }
            }catch {
                //if this fails, it's no biggy.  potentially, we should log it though... don't know how, though...
            }
        }
        unknownSongs = null;

    }
    protected void PrintSongs() {
        if(songs!=null)
        foreach (var song in songs) {
            Response.Write(string.Format(@"<div class=""song"" style=""left: {2}%;top:{3}%;""><table><tr><th>{0}</th></tr><tr><td>{1}</td></tr></table></div>",
                Server.HtmlEncode( song.Song.Artist),
                Server.HtmlEncode( song.Song.Title),
                song.MappedPosition.X,
                song.MappedPosition.Y));
        }
    }
}
