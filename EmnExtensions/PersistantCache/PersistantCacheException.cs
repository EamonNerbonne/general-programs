using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.PersistantCache
{
	public class PersistantCacheException : Exception
	{
		public PersistantCacheException() : base() { }
		public PersistantCacheException(string message) : base(message) { }
		public PersistantCacheException(string message, Exception innerException) : base(message, innerException) { }
	}
}
