using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using System.IO;
using LastFMspider;
using SimilarityMdsLib;
using System.Windows;
using EmnExtensions;
using System.Windows.Media;

namespace LastFmMdsDisplay
{
    public class PositionedSong
    {
        public SongRef Song { get; private set; }
        public Point Position { get; private set; }
        public Point MappedPosition { get; set; }
        public PositionedSong(SongRef song, double x, double y) {
            Song = song;
            MappedPosition=Position = new Point(x, y);
        }
    }

    public class PositionedTracks
    {
        public static void RepositionWithin(IEnumerable<PositionedSong> songs, Rect projectInto) {
            Rect bounds = Rect.Empty;
            foreach (var song in songs)
                bounds.Union(song.Position);
            Matrix transMat = Matrix.Identity;
            transMat.Translate(-bounds.X, -bounds.Y);
            transMat.Scale(projectInto.Width / bounds.Width, projectInto.Height / bounds.Height);
            transMat.Translate(projectInto.X, projectInto.Y);
            foreach (var song in songs)
                song.MappedPosition = transMat.Transform(song.Position);
        }

        public static void PositionSongs(Stream m3uStream, LastFmTools tools, MdsResults mdsPos, out PositionedSong[] songs, out string[] unknown) {
            var playlist = LoadM3U.LoadPlaylistFromM3U(m3uStream, tools);
            PositionSongs(playlist, mdsPos, out songs, out unknown);
        }
        public static void PositionSongs(IEnumerable< ISongInPlaylist> playlistE, MdsResults mdsPos, out PositionedSong[] songs, out string[] unknown) {
            var playlist = playlistE.ToArray();
            var pos = PositionTracks(playlist, mdsPos);
            List<PositionedSong> songsL=new List<PositionedSong>();
            List<string> unknownL=new List<string>();
            for (int i = 0; i < playlist.Length; i++) {
                if (pos[i, 0].IsFinite()) {
                    var songP = ((SongWithId)playlist[i]);
                    songsL.Add(new PositionedSong(songP.songref, pos[i, 0], pos[i, 1]));
                } else {
                    unknownL.Add(playlist[i].HumanLabel);
                }
            }
            songs = songsL.ToArray();
            unknown = unknownL.ToArray();
        }


        public static double[,] PositionTracks(ISongInPlaylist[] playlist, MdsResults mdsPos) {
            int dimCount = mdsPos.Embedding.GetLength(1);
            double[,] songPositions = new double[playlist.Length, dimCount];
            for (int songI = 0; songI < playlist.Length; songI++) {
                var song = playlist[songI];
                song.ComputeDenseId(mdsPos.Mapper);
                if (song.DenseId.HasValue) {
                    int mdsId = song.DenseId.Value;
                    for (int dim = 0; dim < dimCount; dim++) {
                        songPositions[songI, dim] = mdsPos.Embedding[mdsId, dim];
                    }
                } else {
                    for (int dim = 0; dim < dimCount; dim++) {
                        songPositions[songI, dim] = double.NaN;
                    }
                }
            }
            return songPositions;
        }
    }


    public interface ISongInPlaylist
    {
        int? DenseId { get; }
        string HumanLabel { get; }
        void ComputeDenseId(TrackMapper mapper);
    }

    public class SongNotFound : ISongInPlaylist
    {
        public int? DenseId { get { return null; } }
        public string HumanLabel { get; private set; }
        public SongNotFound(string humanlabel) { HumanLabel = humanlabel; }
        public void ComputeDenseId(TrackMapper mapper) { }
    }

    public class SongWithId : ISongInPlaylist
    {
        public int? DenseId { get { return denseID < 0 ? (int?)null : denseID; } }
        public string HumanLabel { get { return songref.Artist + " - " + songref.Title; } }
        public int denseID = -1;
        public Point position; 
        public readonly int sqliteID;
        public readonly SongRef songref;
        public SongWithId(int sqliteid, SongRef songref) {
            this.sqliteID = sqliteid;
            this.songref = songref;
        }

        public void ComputeDenseId(TrackMapper mapper) {
            denseID = mapper.FindDenseID(sqliteID);
        }
    }

    public class LoadM3U
    {

        public static IEnumerable< ISongInPlaylist> LoadPlaylistFromM3U(Stream m3uStream, LastFmTools tools) {
            return LoadExtM3U(m3uStream).Select(psd=>GuessSongRef(psd.HumanLabel,tools));
        }
        public static IEnumerable<ISongInPlaylist> LoadPlaylistFromTextBlock(string text, LastFmTools tools) {
            return 
                text.Split(new[]{'\n'})
                .Select(s=>s.Trim())
                .Where(s=>s.Length>1)
                .Select(humanlabel => GuessSongRef(humanlabel,tools));
        }

        static ISongInPlaylist GuessSongRef(string humanlabel, LastFmTools tools) {
            foreach (var songref in PossibleSongRefs(humanlabel)) {
                int? retval = tools.SimilarSongs.backingDB.LookupTrackID.Execute(songref);
                if (retval != null) {
                    return new SongWithId(retval.Value, songref);
                }
            }
            return new SongNotFound(humanlabel);
        }

        static IEnumerable<SongRef> PossibleSongRefs(string humanlabel) {
            int idxFound = -1;
            while (true) {
                idxFound = humanlabel.IndexOf(" - ", idxFound + 1);
                if (idxFound < 0) yield break;
                yield return SongRef.Create(humanlabel.Substring(0, idxFound), humanlabel.Substring(idxFound + 3));
                //yield return SongRef.Create( humanlabel.Substring(idxFound + 3),humanlabel.Substring(0, idxFound));
            }
        }

        static PartialSongData[] LoadExtM3U(Stream m3uStream) {
            List<PartialSongData> m3usongs = new List<PartialSongData>();
            SongDataFactory.LoadSongsFromM3U(
                m3uStream,
                delegate(ISongData newsong, double completion) {
                    if (newsong is PartialSongData)
                        m3usongs.Add((PartialSongData)newsong);
                },
                Encoding.GetEncoding(1252),
                null
                );
            return m3usongs.ToArray();
        }
    }
}
