using SongDataLib;
using System.Xml.Linq;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization;
namespace LastFMspider
{
	[Serializable]
	public class SongRef
	{
		private string artist;
		public string Artist { get { return artist; } }
		private string title;
		public string Title { get { return title; } }
		public int hashcode;

		public SongRef(string artist, string title) {
			this.artist = string.Intern(artist);//interning saves bucketloads of memory since multiple tracks with the same artist share the same string
			this.title = string.Intern(title);//but it's also a memory leak - no string ever constructed ever leaves memory either, this way...
			hashcode = Artist.ToLowerInvariant().GetHashCode() + Title.ToLowerInvariant().GetHashCode(); 
		}

		public static SongRef Create(SongData song) {
			if(song.performer == null || song.title == null) return null;//TODO - add error handling or simply remove from db?
			return new SongRef(song.performer, song.title);
		}
		public override bool Equals(object obj) {
			if(!(obj is SongRef)) return false;
			SongRef other = ((SongRef)obj);
			return other.Artist.Equals(Artist, StringComparison.InvariantCultureIgnoreCase) && Title.Equals(other.Title, StringComparison.InvariantCultureIgnoreCase);
		}
		public override int GetHashCode() {
			return hashcode;
		}

		public string AudioscrobblerSimilarUrl() {
			return "http://ws.audioscrobbler.com/1.0/track/" + Uri.EscapeDataString(Artist) + "/" + Uri.EscapeDataString(Title) + "/similar.xml";
            //TODO: test ampersands and question marks, I don't trust it!
		}
		public string CacheName() {
			return (Uri.EscapeDataString(Artist.ToLowerInvariant()) + " " + Uri.EscapeDataString(Title.ToLowerInvariant())).Replace("*", "%2A").ToLowerInvariant();
		}
		public static SongRef CreateFromCacheName(string cachename) {
			var parts = cachename.Split(' ');
			return new SongRef(Uri.UnescapeDataString(parts[0]), Uri.UnescapeDataString(parts[1]));
		}

		public string OldCacheName() {
			return (Uri.EscapeDataString(Artist) + " " + Uri.EscapeDataString(Title)).Replace("*", "%2A").ToLowerInvariant();
		}

		/*public string NewCacheName() {
			return hexMd5OfUTF8(artist + "\t" + title);
		}
		static string hexMd5OfUTF8(string input) {
			MD5 md5 = MD5.Create();
			byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
			char[] cHash = new char[hash.Length*2];
			for(int i = 0; i < hash.Length; i++) 
				Convert.ToString(hash[i], 16).CopyTo(0, cHash, 2 * i, 2);
			return new string(cHash);
		}*/

		public static SongRef CreateFromXml(XElement xEl) {
			SongRef retval = new SongRef((string)xEl.Attribute("artist"), (string)xEl.Attribute("title"));
			if(retval.CacheName() != (string)xEl.Attribute("encodedName"))
				throw new Exception("Invalid encodedName - error?");
			return retval;
		}


		public IEnumerable<XAttribute> ToXml() {
			yield return new XAttribute("artist", Artist);
			yield return new XAttribute("title", Title);
			yield return new XAttribute("encodedName", CacheName());
		}
	}
}