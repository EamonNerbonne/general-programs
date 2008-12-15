using SongDataLib;
using System.Xml.Linq;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Runtime.Serialization;
namespace LastFMspider
{
    public static class SongRefUtils
    {
        static int charDiff = (int)'a' - (int)'A';
        /// <summary>
        /// Last.FM only lowercases normal latin characters.
        /// </summary>
        static public string ToLatinLowercase(this string orig) {
            var chars = orig.ToCharArray();
            for (int i = 0; i < chars.Length; i++) {
                if (chars[i] >= 'A' && chars[i] <= 'Z')
                    chars[i] = (char)((int)chars[i] + charDiff);
            }
            return new string(chars);
        }

    }

    [Serializable]
	public class SongRef
	{


		private string artist;
		public string Artist { get { return artist; } }
		private string title;
		public string Title { get { return title; } }
		public readonly int hashcode;


        public static SongRef Create(string artist, string title)
        {
            return Cache<SongRef>.Unique(new SongRef(artist, title), s=>s.OptimalVersion() );
        }


		public static SongRef Create(SongData song) {
			if(song.performer == null || song.title == null) return null;//TODO - add error handling or simply remove from db?
			return Create(song.performer, song.title);
		}
		public override bool Equals(object obj) {
			if(!(obj is SongRef)) return false;
			SongRef other = ((SongRef)obj);
			return other.hashcode == hashcode&& other.Artist.Equals(Artist, StringComparison.InvariantCultureIgnoreCase) && Title.Equals(other.Title, StringComparison.InvariantCultureIgnoreCase);
		}
		public override int GetHashCode() {
			return hashcode;
		}
        public override string ToString() {
            return Artist + " - " + Title;
        }

        private SongRef(string artist, string title) {
            this.artist = artist;
            this.title = title;
            hashcode = Artist.ToLatinLowercase().GetHashCode() + 137 * Title.ToLatinLowercase().GetHashCode();
        }
        private SongRef(string artist, string title, int hashcode) {
            this.artist = artist;
            this.title = title;
            this.hashcode = hashcode;
        }
        private SongRef OptimalVersion() {
            return new SongRef(Cache<string>.Unique(artist, null), Cache<string>.Unique(title, null), hashcode);
        }
        private static class Cache<T>
        {
            public static int nextClearIn = 10000;
            public static double clearScaleFactor = 0.5;
            private static Dictionary<int, WeakReference[]> cache = new Dictionary<int, WeakReference[]>();
            public static T Unique(T item, Func<T, T> optimize) {
                if (nextClearIn <= 0) {
                    var keys = cache.Keys.ToArray();
                    foreach (var key in keys) {
                        var liverefs = cache[key].Where(wk => wk.Target != null).ToArray();
                        if (liverefs.Length == 0)
                            cache.Remove(key);
                        else
                            cache[key] = liverefs;
                    }
                    nextClearIn = (int)(clearScaleFactor * cache.Count);
                } else
                    nextClearIn--;
                if (optimize == null) optimize = x => x;
                int code = item.GetHashCode();
                lock (cache) {
                    if (cache.ContainsKey(code)) {
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
                    } else {
                        cache[code] = new[] { new WeakReference(optimize(item)) };
                        return item;
                    }
                }
            }
        }

	}
}