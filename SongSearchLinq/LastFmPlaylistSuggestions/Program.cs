using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using System.IO;
using LastFMspider;
using EmnExtensions.DebugTools;
using EmnExtensions;
namespace LastFmPlaylistSuggestions
{
    class Program
    {
        static void Main(string[] args) {
            //args.PrintAllDebug();
            //Console.ReadLine();
            RunNew(new LastFmTools(new SongDatabaseConfigFile(false)), args);
        }

        static void RunNew(LastFmTools tools, string[] args) {
            var dir = new DirectoryInfo(@"D:\Data\playlists");
            var m3us = args.Length == 0 ? dir.GetFiles("*.m3u") : args.Select(s => new FileInfo(s)).Where(f => f.Exists);
            DirectoryInfo m3uDir = args.Length == 0 ? tools.DB.DatabaseDirectory.CreateSubdirectory("lists") : new DirectoryInfo(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

            foreach (var m3ufile in m3us) {
                try {
                    ProcessM3U(tools,m3ufile, m3uDir);
                } catch (Exception e) {
                    Console.WriteLine("Unexpected error on processing " + m3ufile);
                    Console.WriteLine(e.ToString());
                }
            }
        }

        static void ProcessM3U(LastFmTools tools,FileInfo m3ufile, DirectoryInfo m3uDir) {
            Console.WriteLine("Trying " + m3ufile.FullName);
            var playlist = LoadExtM3U(m3ufile);
            var known = new List<SongData>();
            var unknown = new List<SongRef>();
            foreach (var song in playlist) {
                SongData bestMatch = null;
                int artistTitleSplitIndex = song.HumanLabel.IndexOf(" - ");
                if (tools.Lookup.dataByPath.ContainsKey(song.SongPath)) bestMatch = tools.Lookup.dataByPath[song.SongPath];
                else {
                    int bestMatchVal = Int32.MaxValue;
                    while (artistTitleSplitIndex != -1) {
                        SongRef songref = SongRef.Create(song.HumanLabel.Substring(0, artistTitleSplitIndex), song.HumanLabel.Substring(artistTitleSplitIndex + 3));
                        if (tools.Lookup.dataByRef.ContainsKey(songref)) {
                            foreach (var songCandidate in tools.Lookup.dataByRef[songref]) {
                                int candidateMatchVal = 100 * Math.Abs(song.Length - songCandidate.Length) + Math.Min(199, Math.Abs(songCandidate.bitrate - 224));
                                if (candidateMatchVal < bestMatchVal) {
                                    bestMatchVal = candidateMatchVal;
                                    bestMatch = songCandidate;
                                }
                            }
                        }
                        artistTitleSplitIndex = song.HumanLabel.IndexOf(" - ", artistTitleSplitIndex + 3);
                    }
                }

                if (bestMatch != null) known.Add(bestMatch);
                else {
                    artistTitleSplitIndex = song.HumanLabel.IndexOf(" - ");
                    if (artistTitleSplitIndex >= 0) unknown.Add(SongRef.Create(song.HumanLabel.Substring(0, artistTitleSplitIndex), song.HumanLabel.Substring(artistTitleSplitIndex + 3)));
                    else Console.WriteLine("Can't deal with: " + song.HumanLabel + "\nat:" + song.SongPath);
                }
            }
            //OK, so we now have the playlist in the var "playlist" with knowns in "known" except for the unknowns, which are in "unknown" as far as possible.

            var playlistSongRefs = new HashSet<SongRef>(known.Select(sd => SongRef.Create(sd)).Where(sr => sr != null).Cast<SongRef>().Concat(unknown));

            var similarTracks =
                from songref in playlistSongRefs//select all "known" songs in the playlist.
                let simlist = tools.SimilarSongs.Lookup(songref)
                where simlist != null
                from simtrack in simlist.similartracks                          //also at least try "unknown songs, who knows, maybe last.fm knows em?
                group simtrack.similarity + 50 by simtrack.similarsong into simGroup    // group all similarity entries by actual song refence (being artist/title)
                let uniquesimtrack = new SimilarTrack { similarsong = simGroup.Key, similarity = simGroup.Sum() - 50 }
                where !playlistSongRefs.Contains(uniquesimtrack.similarsong) //but don't consider those already in the playlist
                orderby uniquesimtrack.similarity descending  //choose most similar tracks first
                select uniquesimtrack;
            similarTracks = similarTracks.ToArray();

            var knownTracks =
                from simtrack in similarTracks
                where tools.Lookup.dataByRef.ContainsKey(simtrack.similarsong)
                select
                   (from songcandidate in tools.Lookup.dataByRef[simtrack.similarsong]
                    orderby Math.Abs(songcandidate.bitrate - 224)
                    select songcandidate).First()
                ;


            FileInfo outputplaylist = new FileInfo(Path.Combine(m3uDir.FullName, Path.GetFileNameWithoutExtension(m3ufile.Name) + "-similar.m3u"));
            using (var stream = outputplaylist.OpenWrite())
            using (var writer = new StreamWriter(stream, Encoding.GetEncoding(1252))) {
                writer.WriteLine("#EXTM3U");
                foreach (var track in knownTracks) {
                    writer.WriteLine("#EXTINF:" + track.Length + "," + track.HumanLabel + "\n" + track.SongPath);
                }
            }
            FileInfo outputsimtracks = new FileInfo(Path.Combine(m3uDir.FullName, Path.GetFileNameWithoutExtension(m3ufile.Name) + "-similar.txt"));
            using (var stream = outputsimtracks.OpenWrite())
            using (var writer = new StreamWriter(stream, Encoding.GetEncoding(1252))) {
                foreach (var track in similarTracks) {
                    writer.WriteLine("{0} {3} {1} - {2}", track.similarity, track.similarsong.Artist, track.similarsong.Title, tools.Lookup.dataByRef.ContainsKey(track.similarsong) ? "" : "***");
                }
            }


        }

        static PartialSongData[] LoadExtM3U(FileInfo m3ufile) {
            List<PartialSongData> m3usongs = new List<PartialSongData>();
            using (var m3uStream = m3ufile.OpenRead()) {
                SongDataFactory.LoadSongsFromM3U(
                    m3uStream,
                    delegate(ISongData newsong, double completion) {
                        if (newsong is PartialSongData)
                            m3usongs.Add((PartialSongData)newsong);
                    },
                    Encoding.GetEncoding(1252),
                    true
                    );
            }
            return m3usongs.ToArray();
        }




        /*void RunOld() {//TODO: this code is old, and specifically assumes that "similarSongs[xyz].similartracks" only contains tracks in "songs"
    //TODO: rewrite to use Lookup instead of the dictionary hardcoded.
    var referredSongsIter =
from similarSongList in similarSongs.Cache.Values
from similarSong in similarSongList.similartracks
let refsong = similarSong.similarsong
where similarSongs.Cache.ContainsKey(refsong)
select refsong;			//so now we have all songs that are similar to at least one other song and are in our song collection.
    var referredSongsArr = referredSongsIter.Distinct().ToArray();
    //duplicates removed and placed into an array.

    Console.WriteLine("Which refer to {0} songs that in turn have references.", referredSongsArr.Length);


    Random r = new Random();
    HashSet<SongRef> selectedSongs = new HashSet<SongRef>();
    if(referredSongsArr.Length < 1000) throw new Exception(string.Format("Impossible to choose 1000 songs, there are only {0} songs referred to!", referredSongsArr.Length));

    while(selectedSongs.Count < 100)
        selectedSongs.Add(referredSongsArr[r.Next(referredSongsArr.Length)]);
    //so we have a basic set of random songs.
    Console.WriteLine("Initial 100 random songs selected");
    HashSet<SongRef> referredBySelected = new HashSet<SongRef>();
    foreach(SongRef song in selectedSongs) {
        foreach(SongRef refsong in similarSongs.Cache[song].similartracks.Select(st => st.similarsong))
            if(!selectedSongs.Contains(refsong) && similarSongs.Cache.ContainsKey(refsong))
                referredBySelected.Add(refsong);
    }

    Console.WriteLine("Which refer to {0} unselected songs", referredBySelected.Count);

    while(selectedSongs.Count < 1000) {
        Console.Write("Picking: ");
        SongRef nextPick = referredBySelected.First();//pick any song referred to
        referredBySelected.Remove(nextPick);
        selectedSongs.Add(nextPick);//add it to the selected songs and remove from the referred songs
        Console.Write("ref:");
        foreach(SongRef refsong in similarSongs.Cache[nextPick].similartracks.Select(st => st.similarsong)) {
            if(!selectedSongs.Contains(refsong) && similarSongs.Cache.ContainsKey(refsong)) {
                referredBySelected.Add(refsong);//add all songs it refers to (but which aren't yet selected) to the referred set.
                Console.Write(".");
            }
        }
        Console.WriteLine();
    }


    //OK so we have 1000 songs which refer hopefully at least somewhat to each other.


    DirectoryInfo songDir = db.DatabaseDirectory.CreateSubdirectory("songs");
    DirectoryInfo simDir = db.DatabaseDirectory.CreateSubdirectory("simil");

    foreach(SongRef song in selectedSongs) {
        Console.WriteLine("Selected " + song.CacheName());
        SongData songmetadata = lookup.dataByRef[song].Where(sd => File.Exists(sd.SongPath)).First();
        string songName = song.CacheName();
        string mp3Path = Path.Combine(songDir.FullName, songName + ".mp3");
        string simPath = Path.Combine(simDir.FullName, songName + ".xml");
        File.Copy(songmetadata.SongPath, mp3Path);
        similarSongs.Cache[song].ToXElement().Save(simPath);
    }
}*/


    }
}
