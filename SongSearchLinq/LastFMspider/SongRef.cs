﻿using SongDataLib;
using System.Xml.Linq;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Runtime.Serialization;
namespace LastFMspider
{
	[Serializable]
	public class SongRef
	{
        private static class Cache<T> 
        {
            public static int nextClearIn=10000;
            public static double clearScaleFactor = 0.5;
            private static Dictionary<int, WeakReference[]> cache = new Dictionary<int, WeakReference[]>();
            public static T Unique(T item,Func<T,T> optimize)
            {
                if (nextClearIn <= 0)
                {
                    var keys = cache.Keys.ToArray();
                    foreach (var key in keys)
                    {
                        var liverefs = cache[key].Where(wk => wk.Target != null).ToArray();
                        if (liverefs.Length == 0)
                            cache.Remove(key);
                        else
                            cache[key] = liverefs;
                    }
                    nextClearIn = (int)(clearScaleFactor * cache.Count);
                }
                else
                    nextClearIn--;
                if (optimize == null) optimize = x => x;
                int code = item.GetHashCode();
                lock (cache)
                {
                    if (cache.ContainsKey(code))
                    {
                        var items = cache[code].Select(w => w.Target).Where(o => o != null).Cast<T>();
                        var list = new List<T>();
                        foreach (var cacheditem in items)
                            if (cacheditem.Equals(item))
                                return cacheditem;
                            else
                                list.Add(cacheditem);
                        list.Add(optimize(item));
                        cache[code] = list.Select(i => new WeakReference(i)).ToArray();
                        return item;
                    }
                    else
                    {
                        cache[code] = new[] { new WeakReference(optimize(item)) };
                        return item;
                    }
                }
            }
        }

		private string artist;
		public string Artist { get { return artist; } }
		private string title;
		public string Title { get { return title; } }
		public readonly int hashcode;

        private SongRef OptimalVersion()
        {
            return new SongRef(Cache<string>.Unique(artist,null), Cache<string>.Unique(title,null), hashcode);
        }

        public static SongRef Create(string artist, string title)
        {
            return Cache<SongRef>.Unique(new SongRef(artist, title), s=>s.OptimalVersion() );
            
        }

		private SongRef(string artist, string title) {
			this.artist = artist;
			this.title = title;
			hashcode = Artist.ToLowerInvariant().GetHashCode() + 137*Title.ToLowerInvariant().GetHashCode(); 
		}
        private SongRef(string artist, string title,int hashcode)
        {
            this.artist = artist;
            this.title = title;
            this.hashcode = hashcode;
        }

		public static SongRef Create(SongData song) {
			if(song.performer == null || song.title == null) return null;//TODO - add error handling or simply remove from db?
			return Create(song.performer, song.title);
		}
		public override bool Equals(object obj) {
			if(!(obj is SongRef)) return false;
			SongRef other = ((SongRef)obj);
			return other.Artist.Equals(Artist, StringComparison.InvariantCultureIgnoreCase) && Title.Equals(other.Title, StringComparison.InvariantCultureIgnoreCase);
		}
		public override int GetHashCode() {
			return hashcode;
		}
        public override string ToString() {
            return Artist + " - " + Title;
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
			return Create(Uri.UnescapeDataString(parts[0]), Uri.UnescapeDataString(parts[1]));
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
			return Create(retval.artist,retval.title);
		}


		public IEnumerable<XAttribute> ToXml() {
			yield return new XAttribute("artist", Artist);
			yield return new XAttribute("title", Title);
			yield return new XAttribute("encodedName", CacheName());
		}
	}
}