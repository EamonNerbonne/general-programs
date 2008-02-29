using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EamonExtensionsLinq.PersistantCache
{
	public class DelegateCacheMapper<TKey,TItem>:IPersistantCacheMapper<TKey,TItem> where TItem:class
	{
		public Func<TKey, string> keyToStringDelegate;
		public Func<TKey,Stream, TItem> loadDelegate;
		public Action<TItem, Stream> storeDelegate;
		Func<TKey, TItem> functionToCache;
		public DelegateCacheMapper(Func<TKey, TItem> functionToCache, Func<TKey, string> keyToStringDelegate, Func<TKey,Stream, TItem> loadDelegate, Action<TItem, Stream> storeDelegate) {
			this.keyToStringDelegate = keyToStringDelegate; this.loadDelegate = loadDelegate; this.storeDelegate = storeDelegate; this.functionToCache = functionToCache;
		}

		public TItem Evaluate(TKey key) { return functionToCache(key); }
		public string KeyToString(TKey key) { return keyToStringDelegate(key); }
		public TItem LoadItem(TKey key, Stream from) { return loadDelegate(key,from); }
		public void StoreItem(TItem item, Stream to) { storeDelegate(item, to); }

	}
}
