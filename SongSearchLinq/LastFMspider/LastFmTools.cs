using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using System.IO;

namespace LastFMspider
{
    public class LastFmTools
    {
        public SongSimilarityCache SimilarSongs {get; protected set;}
        public SongDatabaseConfigFile ConfigFile {get;protected set;}
        public SimpleSongDB DB{get;protected set;}
        public SongDataLookups Lookup { get; protected set; }

        public LastFmTools(SongDatabaseConfigFile configFile) {
            ConfigFile = configFile;
        }

        public void UseDB() {
            if (DB == null) 
                DB= new SimpleSongDB(ConfigFile, null);
            
        }
        public void UseSimilarSongs() {
            if (SimilarSongs == null) 
            SimilarSongs = new SongSimilarityCache(ConfigFile);
        }
        public void UseLookup() {
            if (Lookup == null) {
                UseDB();
                Lookup=new SongDataLookups(DB.Songs, null);
            }
        }

        public void UnloadLookup() {
            Lookup = null;
        }

        public void UnloadDB() {
            UnloadLookup();
            DB = null;
        }

        public void PrecacheAudioscrobbler() {
            UseSimilarSongs();
            UseDB();
            UseLookup();
            Console.WriteLine("Loading song database...");
            if (DB.InvalidDataCount != 0) Console.WriteLine("Ignored {0} songs with unknown tags (should be 0).", DB.InvalidDataCount);
            Console.WriteLine("Taking those {0} songs and indexing em by artist/title...", DB.Songs.Count);
            SongRef[] songsToDownload = Lookup.dataByRef.Keys.ToArray();
            UnloadDB();
            System.GC.Collect();
            Console.WriteLine("Downloading Last.fm similar tracks...");
            int progressCount = 0;
            int total = songsToDownload.Length;
            long similarityCount = 0;
            int hits = 0;
            foreach (SongRef songref in songsToDownload) {
                try {
                    progressCount++;
                    var similar = SimilarSongs.Lookup(songref);//precache the last.fm data.  unsure - NOT REALLY necessary?
                    int newSimilars = similar.similartracks == null ? 0 : similar.similartracks.Length;
                    similarityCount += newSimilars;
                    if (similar != null)
                        hits++;
                    Console.WriteLine("{0,3} - tot={4} in hits={5}, with relTo={3} in \"{1} - {2}\"",
                        100 * progressCount / (double)total,
                        songref.Artist,
                        songref.Title,
                        newSimilars,
                        (double)similarityCount,
                        hits);

                } catch (Exception e) {
                    Console.WriteLine("Exception: {0}", e.ToString());
                }//ignore all errors.
            }
            Console.WriteLine("Done precaching.");
        }

        void RunNew(string[] args) {
            UseSimilarSongs();
            var dir = new DirectoryInfo(@"C:\Program Files\Winamp\Plugins\MEXP\Users\Standard\-quicklist");
            var m3us = args.Length == 0 ? dir.GetFiles("*.m3u") : args.Select(s => new FileInfo(s)).Where(f => f.Exists);
            DirectoryInfo m3uDir = args.Length == 0 ? DB.DatabaseDirectory.CreateSubdirectory("lists") : new DirectoryInfo(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

            foreach (var m3ufile in m3us) {
                try {
                    ProcessM3U(m3ufile, m3uDir);
                } catch (Exception e) {
                    Console.WriteLine("Unexpected error on processing " + m3ufile);
                    Console.WriteLine(e.ToString());
                }
            }
        }

        void ProcessM3U(FileInfo m3ufile, DirectoryInfo m3uDir) {
            UseLookup();
            UseSimilarSongs();
            Console.WriteLine("Trying " + m3ufile.FullName);
            var playlist = LoadExtM3U(m3ufile);
            var known = new List<SongData>();
            var unknown = new List<SongRef>();
            foreach (var song in playlist) {
                SongData bestMatch = null;
                int artistTitleSplitIndex = song.HumanLabel.IndexOf(" - ");
                if (Lookup.dataByPath.ContainsKey(song.SongPath)) bestMatch = Lookup.dataByPath[song.SongPath];
                else {
                    int bestMatchVal = Int32.MaxValue;
                    while (artistTitleSplitIndex != -1) {
                        SongRef songref = SongRef.Create(song.HumanLabel.Substring(0, artistTitleSplitIndex), song.HumanLabel.Substring(artistTitleSplitIndex + 3));
                        if (Lookup.dataByRef.ContainsKey(songref)) {
                            foreach (var songCandidate in Lookup.dataByRef[songref]) {
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
                let simlist = SimilarSongs.Lookup(songref)
                where simlist != null
                from simtrack in SimilarSongs.Lookup(songref).similartracks                          //also at least try "unknown songs, who knows, maybe last.fm knows em?
                group simtrack.similarity + 50 by simtrack.similarsong into simGroup    // group all similarity entries by actual song refence (being artist/title)
                let uniquesimtrack = new SimilarTrack { similarsong = simGroup.Key, similarity = simGroup.Sum() - 50 }
                where !playlistSongRefs.Contains(uniquesimtrack.similarsong) //but don't consider those already in the playlist
                orderby uniquesimtrack.similarity descending  //choose most similar tracks first
                select uniquesimtrack;
            similarTracks = similarTracks.ToArray();

            var knownTracks =
                from simtrack in similarTracks
                where Lookup.dataByRef.ContainsKey(simtrack.similarsong)
                select
                   (from songcandidate in Lookup.dataByRef[simtrack.similarsong]
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
                    writer.WriteLine("{0} {3} {1} - {2}", track.similarity, track.similarsong.Artist, track.similarsong.Title, Lookup.dataByRef.ContainsKey(track.similarsong) ? "" : "***");
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

        public void RunStats() {
            UseSimilarSongs();
            var stats = SimilarSongs.LookupDbStats();

            Console.WriteLine("Found {0} Referenced songs, of which {1} have stats downloaded.", stats.Length, stats.Where(s => s.LookupTimestamp != null).Count());
            Console.WriteLine("Sorting by # of references...");
            Random r = new Random((int)DateTime.Now.Ticks / 50);
            var randarray = Enumerable.Repeat(0, stats.Length)
                .Select(zero => r.Next())
                .ToArray();
            Array.Sort(randarray, stats);
            //Array.Sort(stats, (a, b) => b.TimesReferenced.CompareTo(a.TimesReferenced));
            Console.WriteLine("Showing a few...");

            foreach (var stat in stats) {
                Console.WriteLine("{1} {0}, {2}", stat.SongRef.ToString(), stat.LookupTimestamp == null ? "!" : " ", stat.TimesReferenced);
                try { SimilarSongs.Lookup(stat.SongRef); } catch (Exception e) {
                    try {
                        Console.WriteLine("Error in {0}: {1}: {2}", stat.SongRef.ToString(), e.Message, e.StackTrace);
                    } catch { }
                }
                //shown++;

                //if (shown % 20 == 0) { Console.WriteLine("Press any key for more"); Console.ReadKey(); }
            }


        }



    }
}
