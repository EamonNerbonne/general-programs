using System;

namespace SongDataLib
{
	public class SongDataConfigException : Exception
	{
		public SongDataConfigException(SongDataConfigFile databaseConfigFile, string message) : base("Error while parsing " + databaseConfigFile.configPathReadable + ":\n" + message) { }
		public SongDataConfigException(SongDataConfigFile databaseConfigFile, Exception innerException) : base("Error while parsing " + databaseConfigFile.configPathReadable + ".", innerException) { }
	}
}
