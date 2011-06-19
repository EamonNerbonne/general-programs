using SongDataLib;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
namespace SongDataLib {
	public static class SongRefUtils {
		const int charDiff = 'a' - 'A';
		/// <summary>
		/// Last.FM only lowercases normal latin characters.
		/// </summary>
		static public string ToLatinLowercase(this string orig) {
			var retval = new StringBuilder(orig.Length) { Length = orig.Length };

			for (int i = 0; i < orig.Length; i++) {
				char c = orig[i];
				retval[i] =
					(c >= 'A' && c <= 'Z')
					? (char)(c + charDiff)
					: c;
			}
			return retval.ToString();
		}

	}

	[Serializable]
	public sealed class SongRef {
		private readonly string artist;
		public string Artist { get { return artist; } }
		public string GetLowerArtist() { return artist.ToLatinLowercase(); }
		private readonly string title;
		public string Title { get { return title; } }
		public string GetLowerTitle() { return title.ToLatinLowercase(); }
		readonly int hashcode;


		public static SongRef Create(string artist, string title) { return new SongRef(artist, title); } // Cache<SongRef>.Unique(new SongRef(artist, title), s => s.OptimalVersion()); }
		public static IEnumerable<SongRef> PossibleSongRefs(string label) {
			for (int artistTitleSplitIndex = label.IndexOf(" - "); artistTitleSplitIndex != -1; artistTitleSplitIndex = label.IndexOf(" - ", artistTitleSplitIndex + 3))
				yield return Create(label.Substring(0, artistTitleSplitIndex), label.Substring(artistTitleSplitIndex + 3));
		}

		public override bool Equals(object obj) {
			if (!(obj is SongRef))
				return false;
			SongRef other = ((SongRef)obj);
			return other.hashcode == hashcode && other.GetLowerArtist().Equals(GetLowerArtist()) && GetLowerTitle().Equals(other.GetLowerTitle());
		}
		public override int GetHashCode() { return hashcode; }
		public override string ToString() { return Artist + " - " + Title; }

		private SongRef(string artist, string title) {
			this.artist = artist;
			this.title = title;
			hashcode = GetLowerArtist().GetHashCode() + 137 * GetLowerTitle().GetHashCode();
		}
		private SongRef(string artist, string title, int hashcode) {
			this.artist = artist;
			this.title = title;
			this.hashcode = hashcode;
		}

		private SongRef OptimalVersion() {
			return new SongRef(Cache<string>.Unique(artist, null), Cache<string>.Unique(title, null), hashcode);
		}
		private static class Cache<T> {
			public static int nextClearIn = 1000000;
			public const double clearScaleFactor = 0.5;
			private static readonly Dictionary<int, WeakReference[]> cache = new Dictionary<int, WeakReference[]>();
			public static T Unique(T item, Func<T, T> optimize) {
				string.Equals("", "", StringComparison.OrdinalIgnoreCase);
				lock (cache) {
					if (nextClearIn <= 0) {
						GC.Collect();
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
					if (optimize == null)
						optimize = x => x;
					int code = item.GetHashCode();
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