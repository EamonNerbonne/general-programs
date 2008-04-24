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
    public struct Timestamped<TItem>
    {
        public TItem Item;
        public DateTime Timestamp;
    }

	public class PersistantCache<TKey,TItem> where TItem : class 
        //TODO: should be made more generic wrt key->value mappings and the whole filename/string/storage/serialization business
        //TODO: consider usecases of persitant caches backed by other caches backed by memoization etc.
        //TODO: consider multiple backends like sqlite, filesystem, BDB etc
        //TODO: in filesystem storage, consider multiple directories for speed.
	{

		static char[] invalidKeyChars = Path.GetInvalidFileNameChars();
		public PersistantCache(DirectoryInfo cacheDir, string ext, IPersistantCacheMapper<TKey,TItem>mapper) {
			if(!ext.StartsWith(".")) throw new PersistantCacheException("extension must start with a '.'");
			this.cacheDir = cacheDir;
            this.filesDir = cacheDir.CreateSubdirectory("files");
			this.ext = ext;
			this.mapper = mapper;
			maxKeyLength = 259 - (cacheDir.FullName.Length + 1)-ext.Length;
		}
		
		public readonly IPersistantCacheMapper<TKey,TItem> mapper;
		readonly DirectoryInfo cacheDir;
        readonly DirectoryInfo filesDir;
		readonly string ext;
		int maxKeyLength;
		//int lastSave = 0;
		//public int storeEach = 10000;
        
        Dictionary<TKey, Timestamped<TItem>> memCache = new Dictionary<TKey, Timestamped<TItem>>(); //used to be serialized at:Path.Combine(cacheDir.FullName,"%%%"+ext+".bin")
		private void AssertKeyStringValid(string key) {
			if(key.Length > maxKeyLength) throw new PersistantCacheException("Key too long, may be at most 259 chars including directory, directory separator, and extension.\n In this case that means at most " + maxKeyLength + " chars long.");
			if(key.IndexOfAny(invalidKeyChars)>=0) {
				char nogood = key[key.IndexOfAny(invalidKeyChars)];
				throw new PersistantCacheException("Key may not contain invalid filename chars - specifically no '" + nogood + "'.");
			}
		}

		private FileInfo getFileStoreLocation(string key) {
			AssertKeyStringValid(key);
			return new FileInfo(Path.Combine(filesDir.FullName, key + ext));//TODO: provide fallback?
		}
		public IEnumerable<string> GetDiskCacheContents() {return filesDir.GetFiles("*" + ext).Select(fi=>fi.Name.Substring(0,fi.Name.Length - ext.Length)) ; }
		public Dictionary<TKey,Timestamped<TItem>> MemoryCache { get { return memCache; } }
		public Timestamped<TItem> Lookup(TKey key) { return Lookup(key, mapper.Evaluate); }
        public Timestamped<TItem> Lookup(TKey key, Func<TKey, TItem> customEvaluator)
        {
			Timestamped<TItem> item;

			if(memCache.TryGetValue(key, out item))
				return item;

			string keyString = mapper.KeyToString(key);

			FileInfo loc = getFileStoreLocation(keyString);
			if(loc.Exists) {
                using (Stream s=loc.OpenRead())
                item = new Timestamped<TItem>
                {
                    Item = mapper.LoadItem(key, s),
                    Timestamp = loc.LastWriteTimeUtc
                };
			} else {
                item = new Timestamped<TItem>
                {
                    Item = customEvaluator(key),
                    Timestamp = DateTime.UtcNow
                }; 
				using(Stream s = getFileStoreLocation(keyString).OpenWrite()) {
					mapper.StoreItem(item.Item, s);
				}
			}
			memCache[key] = item;
			//if(memCache.Count > lastSave + storeEach) StoreQuickLoad();
			return item;
		}
        public void DeleteItem(TKey key) {
            FileInfo loc = getFileStoreLocation(mapper.KeyToString(key));
            if (loc.Exists)
                loc.Delete();
            if(memCache.ContainsKey(key))
                memCache.Remove(key);
        }
	}
}
