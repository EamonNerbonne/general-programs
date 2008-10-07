using System;
using System.Collections.Generic;

namespace SongDataLib
{
	public delegate string NormalizerDelegate(string str);

	public interface ISongSearcher
	{
		void Init(SongDB db);
		SearchResult Query(byte[] query);
		//SearchResult CompleteQuery(string query, BitArray filter, BitArray result);
	}
}
