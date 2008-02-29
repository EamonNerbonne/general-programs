using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using EamonExtensionsLinq.FastSerializer;

namespace EamonExtensionsLinq.PersistantCache
{

	public class PersistantCache<TKey,TItem> where TItem : class
	{

		static char[] invalidKeyChars = Path.GetInvalidFileNameChars();
		public PersistantCache(DirectoryInfo cacheDir, string ext, IPersistantCacheMapper<TKey,TItem>mapper) {
			if(!ext.StartsWith(".")) throw new PersistantCacheException("extension must start with a '.'");
			this.cacheDir = cacheDir;
			this.ext = ext;
			this.mapper = mapper;
			maxKeyLength = 259 - (cacheDir.FullName.Length + 1)-ext.Length;
			Load();
		}
		~PersistantCache() {
			StoreQuickLoad();
		}
		void Load() {
            return;
			FileInfo store = new FileInfo(Path.Combine(cacheDir.FullName,"%%%"+ext+".bin"));
			if(store.Exists) {
				try {
					using(Stream s = store.OpenRead()) {
						BinaryFormatter bin = new BinaryFormatter();
						memCache= (Dictionary<TKey, TItem> ) bin.Deserialize(s);
						lastSave = memCache.Count;
					}
				} catch { }
			}
		}
		public void StoreQuickLoad() {
            return;
			FileInfo store = new FileInfo(Path.Combine(cacheDir.FullName, "%%%" + ext + ".bin"));
				try {
					lastSave = memCache.Count;
					using(Stream s = store.OpenWrite()) {
//						SerializationWriter w= SerializationWriter.GetWriter();
						//w.Write(memCache);
						BinaryFormatter bin = new BinaryFormatter();
						bin.Serialize(s, memCache);
					}
				} catch { }
		}
		
		public readonly IPersistantCacheMapper<TKey,TItem> mapper;
		readonly DirectoryInfo cacheDir;
		readonly string ext;
		int maxKeyLength;
		int lastSave = 0;
		public int storeEach = 10000;
		Dictionary<TKey, TItem> memCache = new Dictionary<TKey, TItem>();
		private void AssertKeyStringValid(string key) {
			if(key.Length > maxKeyLength) throw new PersistantCacheException("Key too long, may be at most 259 chars including directory, directory separator, and extension.\n In this case that means at most " + maxKeyLength + " chars long.");
			if(key.IndexOfAny(invalidKeyChars)>=0) {
				char nogood = key[key.IndexOfAny(invalidKeyChars)];
				throw new PersistantCacheException("Key may not contain invalid filename chars - specifically no '" + nogood + "'.");
			}
		}

		private FileInfo getFileStoreLocation(string key) {
			AssertKeyStringValid(key);
			return new FileInfo(Path.Combine(cacheDir.FullName, key + ext));
		}
		public IEnumerable<string> GetDiskCacheContents() {return cacheDir.GetFiles("*" + ext).Select(fi=>fi.Name.Substring(0,fi.Name.Length - ext.Length)) ; }
		public Dictionary<TKey,TItem> MemoryCache { get { return memCache; } }
		public TItem Lookup(TKey key) { return Lookup(key, mapper.Evaluate); }
		public TItem Lookup(TKey key, Func<TKey,TItem> customEvaluator) {
			TItem item;

			if(memCache.TryGetValue(key, out item))
				return item;

			string keyString = mapper.KeyToString(key);

			FileInfo loc = getFileStoreLocation(keyString);
			if(loc.Exists) {
				item = mapper.LoadItem(key,loc.OpenRead());
			} else {
				item = customEvaluator(key);
				using(Stream s = getFileStoreLocation(keyString).OpenWrite()) {
					mapper.StoreItem(item, s);
				}
			}
			memCache[key] = item;
			if(memCache.Count > lastSave + storeEach) StoreQuickLoad();
			return item;
		}
	}
}
