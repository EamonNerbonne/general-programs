using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EmnExtensions.PersistantCache
{
	public interface IPersistantCacheMapper<TKey,TItem> where TItem : class
	{
		string KeyToString(TKey key);
		TItem Evaluate(TKey key);//TODO: combine storeitem and evaluate

		void StoreItem(TItem item, Stream to);
		TItem LoadItem(TKey key,Stream from);
	}
}
