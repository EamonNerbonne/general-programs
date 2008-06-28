using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LastFmPlaylistSuggestions
{
    class Program
    {
        static void Main(string[] args) {
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
